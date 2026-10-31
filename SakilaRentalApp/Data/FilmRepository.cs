using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc.Rendering;
using SakilaRentalApp.Models;
using System.Data;

namespace SakilaRentalApp.Data
{
    public class FilmRepository
    {
        private readonly DatabaseHelper _db;

        public FilmRepository(DatabaseHelper db)
        {
            _db = db;
        }

        /// <summary>
        /// Obtiene el catálogo de películas con cálculo en tiempo real de copias en inventario y disponibilidad.
        /// Aplica filtro de eliminación lógica (is_deleted = 0) para usuarios regulares.
        /// </summary>
        public async Task<(List<Film> Peliculas, int TotalRegistros)> GetCatalogoAsync(
            string? buscar = null,
            string? rating = null,
            string? disponibilidad = null,
            bool incluirEliminadas = false,
            int pagina = 1,
            int tamanoPagina = 12)
        {
            var peliculas = new List<Film>();
            int totalRegistros = 0;

            var whereClauses = new List<string>();

            if (!incluirEliminadas)
            {
                whereClauses.Add("f.is_deleted = 0");
            }

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                whereClauses.Add("f.title LIKE @Buscar");
            }

            if (!string.IsNullOrWhiteSpace(rating))
            {
                whereClauses.Add("f.rating = @Rating");
            }

            string whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            string havingSql = "";
            if (disponibilidad == "disponibles")
            {
                havingSql = "HAVING (COUNT(DISTINCT i.inventory_id) - COUNT(DISTINCT CASE WHEN r.rental_id IS NOT NULL AND r.return_date IS NULL THEN i.inventory_id END)) > 0";
            }
            else if (disponibilidad == "agotadas")
            {
                havingSql = "HAVING (COUNT(DISTINCT i.inventory_id) - COUNT(DISTINCT CASE WHEN r.rental_id IS NOT NULL AND r.return_date IS NULL THEN i.inventory_id END)) = 0";
            }

            // Consulta de conteo para paginación
            string countSql = $@"
                WITH DisponibilidadCTE AS (
                    SELECT 
                        f.film_id
                    FROM film f
                    LEFT JOIN inventory i ON f.film_id = i.film_id
                    LEFT JOIN rental r ON i.inventory_id = r.inventory_id AND r.return_date IS NULL
                    {whereSql}
                    GROUP BY f.film_id
                    {havingSql}
                )
                SELECT COUNT(*) FROM DisponibilidadCTE;";

            // Paginación y proyección (evita agrupar por campos de tipo TEXT)
            string dataSql = $@"
                WITH PagedFilms AS (
                    SELECT 
                        f.film_id,
                        f.title,
                        COUNT(DISTINCT i.inventory_id) AS total_copias,
                        (COUNT(DISTINCT i.inventory_id) - COUNT(DISTINCT CASE WHEN r.rental_id IS NOT NULL AND r.return_date IS NULL THEN i.inventory_id END)) AS copias_disponibles
                    FROM film f
                    LEFT JOIN inventory i ON f.film_id = i.film_id
                    LEFT JOIN rental r ON i.inventory_id = r.inventory_id AND r.return_date IS NULL
                    {whereSql}
                    GROUP BY f.film_id, f.title
                    {havingSql}
                    ORDER BY f.title ASC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                )
                SELECT 
                    f.film_id,
                    f.title,
                    f.description,
                    f.release_year,
                    f.language_id,
                    l.name AS language_name,
                    f.rental_duration,
                    f.rental_rate,
                    f.length,
                    f.replacement_cost,
                    f.rating,
                    f.special_features,
                    f.is_deleted,
                    f.last_update,
                    pf.total_copias,
                    pf.copias_disponibles
                FROM PagedFilms pf
                INNER JOIN film f ON pf.film_id = f.film_id
                INNER JOIN language l ON f.language_id = l.language_id
                ORDER BY f.title ASC;";

            using var conn = await _db.GetOpenConnectionAsync();

            // 1. Obtener conteo total
            using (var countCmd = new SqlCommand(countSql, conn))
            {
                if (!string.IsNullOrWhiteSpace(buscar))
                {
                    countCmd.Parameters.Add("@Buscar", SqlDbType.VarChar).Value = $"%{buscar.Trim()}%";
                }
                if (!string.IsNullOrWhiteSpace(rating))
                {
                    countCmd.Parameters.Add("@Rating", SqlDbType.VarChar).Value = rating.Trim();
                }

                totalRegistros = (int)(await countCmd.ExecuteScalarAsync() ?? 0);
            }

            // 2. Obtener datos de la página
            using (var dataCmd = new SqlCommand(dataSql, conn))
            {
                if (!string.IsNullOrWhiteSpace(buscar))
                {
                    dataCmd.Parameters.Add("@Buscar", SqlDbType.VarChar).Value = $"%{buscar.Trim()}%";
                }
                if (!string.IsNullOrWhiteSpace(rating))
                {
                    dataCmd.Parameters.Add("@Rating", SqlDbType.VarChar).Value = rating.Trim();
                }

                int offset = (pagina - 1) * tamanoPagina;
                dataCmd.Parameters.Add("@Offset", SqlDbType.Int).Value = offset;
                dataCmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = tamanoPagina;

                using var reader = await dataCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    peliculas.Add(MapFilmWithAvailability(reader));
                }
            }

            return (peliculas, totalRegistros);
        }

        public async Task<Film?> GetByIdAsync(int filmId)
        {
            const string sql = @"
                SELECT 
                    f.film_id,
                    f.title,
                    f.description,
                    f.release_year,
                    f.language_id,
                    l.name AS language_name,
                    f.original_language_id,
                    f.rental_duration,
                    f.rental_rate,
                    f.length,
                    f.replacement_cost,
                    f.rating,
                    f.special_features,
                    f.is_deleted,
                    f.last_update,
                    (SELECT COUNT(*) FROM inventory i WHERE i.film_id = f.film_id) AS total_copias,
                    (SELECT COUNT(*) FROM inventory i WHERE i.film_id = f.film_id AND i.inventory_id NOT IN (
                        SELECT r.inventory_id FROM rental r WHERE r.return_date IS NULL
                    )) AS copias_disponibles
                FROM film f
                INNER JOIN language l ON f.language_id = l.language_id
                WHERE f.film_id = @FilmId;";

            using var conn = await _db.GetOpenConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@FilmId", SqlDbType.Int).Value = filmId;

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapFilmWithAvailability(reader);
            }
            return null;
        }

        public async Task<int> CreateAsync(Film film)
        {
            const string sql = @"
                INSERT INTO film (
                    title, description, release_year, language_id, original_language_id,
                    rental_duration, rental_rate, length, replacement_cost, rating,
                    special_features, is_deleted, last_update
                )
                VALUES (
                    @Title, @Description, @ReleaseYear, @LanguageId, @OriginalLanguageId,
                    @RentalDuration, @RentalRate, @Length, @ReplacementCost, @Rating,
                    @SpecialFeatures, @IsDeleted, GETDATE()
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var conn = await _db.GetOpenConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@Title", SqlDbType.VarChar, 255).Value = film.Title.Trim().ToUpper();
            cmd.Parameters.Add("@Description", SqlDbType.Text).Value = (object?)film.Description ?? DBNull.Value;
            cmd.Parameters.Add("@ReleaseYear", SqlDbType.VarChar, 4).Value = (object?)film.ReleaseYear ?? DBNull.Value;
            cmd.Parameters.Add("@LanguageId", SqlDbType.TinyInt).Value = film.LanguageId;
            cmd.Parameters.Add("@OriginalLanguageId", SqlDbType.TinyInt).Value = (object?)film.OriginalLanguageId ?? DBNull.Value;
            cmd.Parameters.Add("@RentalDuration", SqlDbType.TinyInt).Value = film.RentalDuration;

            var pRate = cmd.Parameters.Add("@RentalRate", SqlDbType.Decimal);
            pRate.Precision = 4;
            pRate.Scale = 2;
            pRate.Value = film.RentalRate;

            cmd.Parameters.Add("@Length", SqlDbType.SmallInt).Value = (object?)film.Length ?? DBNull.Value;

            var pCost = cmd.Parameters.Add("@ReplacementCost", SqlDbType.Decimal);
            pCost.Precision = 5;
            pCost.Scale = 2;
            pCost.Value = film.ReplacementCost;

            cmd.Parameters.Add("@Rating", SqlDbType.VarChar, 10).Value = film.Rating;
            cmd.Parameters.Add("@SpecialFeatures", SqlDbType.VarChar, 255).Value = (object?)film.SpecialFeatures ?? DBNull.Value;
            cmd.Parameters.Add("@IsDeleted", SqlDbType.Bit).Value = film.IsDeleted;

            return (int)(await cmd.ExecuteScalarAsync() ?? 0);
        }

        public async Task<bool> UpdateAsync(Film film)
        {
            const string sql = @"
                UPDATE film
                SET 
                    title = @Title,
                    description = @Description,
                    release_year = @ReleaseYear,
                    language_id = @LanguageId,
                    rental_duration = @RentalDuration,
                    rental_rate = @RentalRate,
                    length = @Length,
                    replacement_cost = @ReplacementCost,
                    rating = @Rating,
                    special_features = @SpecialFeatures,
                    is_deleted = @IsDeleted,
                    last_update = GETDATE()
                WHERE film_id = @FilmId;";

            using var conn = await _db.GetOpenConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@FilmId", SqlDbType.Int).Value = film.FilmId;
            cmd.Parameters.Add("@Title", SqlDbType.VarChar, 255).Value = film.Title.Trim().ToUpper();
            cmd.Parameters.Add("@Description", SqlDbType.Text).Value = (object?)film.Description ?? DBNull.Value;
            cmd.Parameters.Add("@ReleaseYear", SqlDbType.VarChar, 4).Value = (object?)film.ReleaseYear ?? DBNull.Value;
            cmd.Parameters.Add("@LanguageId", SqlDbType.TinyInt).Value = film.LanguageId;
            cmd.Parameters.Add("@RentalDuration", SqlDbType.TinyInt).Value = film.RentalDuration;

            var pRate = cmd.Parameters.Add("@RentalRate", SqlDbType.Decimal);
            pRate.Precision = 4;
            pRate.Scale = 2;
            pRate.Value = film.RentalRate;

            cmd.Parameters.Add("@Length", SqlDbType.SmallInt).Value = (object?)film.Length ?? DBNull.Value;

            var pCost = cmd.Parameters.Add("@ReplacementCost", SqlDbType.Decimal);
            pCost.Precision = 5;
            pCost.Scale = 2;
            pCost.Value = film.ReplacementCost;

            cmd.Parameters.Add("@Rating", SqlDbType.VarChar, 10).Value = film.Rating;
            cmd.Parameters.Add("@SpecialFeatures", SqlDbType.VarChar, 255).Value = (object?)film.SpecialFeatures ?? DBNull.Value;
            cmd.Parameters.Add("@IsDeleted", SqlDbType.Bit).Value = film.IsDeleted;

            int affected = await cmd.ExecuteNonQueryAsync();
            return affected > 0;
        }

        /// <summary>
        /// Reto 2: Eliminación Lógica.
        /// Alterna el estado de is_deleted sin eliminar el registro físico ni perjudicar alquileres históricos.
        /// </summary>
        public async Task<bool> ToggleSoftDeleteAsync(int filmId)
        {
            const string sql = @"
                UPDATE film 
                SET is_deleted = CASE WHEN is_deleted = 1 THEN 0 ELSE 1 END,
                    last_update = GETDATE()
                WHERE film_id = @FilmId;";

            using var conn = await _db.GetOpenConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@FilmId", SqlDbType.Int).Value = filmId;

            int affected = await cmd.ExecuteNonQueryAsync();
            return affected > 0;
        }

        public async Task<List<SelectListItem>> GetLanguagesAsync()
        {
            var list = new List<SelectListItem>();
            const string sql = "SELECT language_id, name FROM language ORDER BY name;";

            using var conn = await _db.GetOpenConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new SelectListItem
                {
                    Value = reader.GetByte(0).ToString(),
                    Text = reader.GetString(1).Trim()
                });
            }

            return list;
        }

        private static Film MapFilmWithAvailability(SqlDataReader reader)
        {
            return new Film
            {
                FilmId = reader.GetInt32(reader.GetOrdinal("film_id")),
                Title = reader.GetString(reader.GetOrdinal("title")),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                ReleaseYear = reader.IsDBNull(reader.GetOrdinal("release_year")) ? null : reader.GetString(reader.GetOrdinal("release_year")),
                LanguageId = reader.GetByte(reader.GetOrdinal("language_id")),
                LanguageName = reader.GetString(reader.GetOrdinal("language_name")),
                RentalDuration = reader.GetByte(reader.GetOrdinal("rental_duration")),
                RentalRate = reader.GetDecimal(reader.GetOrdinal("rental_rate")),
                Length = reader.IsDBNull(reader.GetOrdinal("length")) ? null : reader.GetInt16(reader.GetOrdinal("length")),
                ReplacementCost = reader.GetDecimal(reader.GetOrdinal("replacement_cost")),
                Rating = reader.IsDBNull(reader.GetOrdinal("rating")) ? "PG" : reader.GetString(reader.GetOrdinal("rating")),
                SpecialFeatures = reader.IsDBNull(reader.GetOrdinal("special_features")) ? null : reader.GetString(reader.GetOrdinal("special_features")),
                IsDeleted = reader.GetBoolean(reader.GetOrdinal("is_deleted")),
                LastUpdate = reader.GetDateTime(reader.GetOrdinal("last_update")),
                TotalCopias = reader.GetInt32(reader.GetOrdinal("total_copias")),
                CopiasDisponibles = reader.GetInt32(reader.GetOrdinal("copias_disponibles"))
            };
        }
    }
}
