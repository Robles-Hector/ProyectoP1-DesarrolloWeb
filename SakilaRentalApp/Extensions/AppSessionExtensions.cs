namespace SakilaRentalApp.Extensions
{
    public static class SessionKeys
    {
        public const string UsuarioId = "UsuarioId";
        public const string Username = "Username";
        public const string Rol = "Rol";
        public const string NombreCompleto = "NombreCompleto";
        public const string CustomerId = "CustomerId";
        public const string StaffId = "StaffId";
    }

    public static class AppSessionExtensions
    {
        public static bool EstaAutenticado(this ISession session)
        {
            return session.GetInt32(SessionKeys.UsuarioId).HasValue;
        }

        public static string? GetUsername(this ISession session)
        {
            return session.GetString(SessionKeys.Username);
        }

        public static string? GetRol(this ISession session)
        {
            return session.GetString(SessionKeys.Rol);
        }

        public static string? GetNombreCompleto(this ISession session)
        {
            return session.GetString(SessionKeys.NombreCompleto);
        }

        public static int? GetCustomerId(this ISession session)
        {
            return session.GetInt32(SessionKeys.CustomerId);
        }

        public static byte? GetStaffId(this ISession session)
        {
            int? val = session.GetInt32(SessionKeys.StaffId);
            return val.HasValue ? (byte)val.Value : null;
        }

        public static bool EsAdmin(this ISession session)
        {
            return string.Equals(session.GetString(SessionKeys.Rol), "Administrador", StringComparison.OrdinalIgnoreCase);
        }

        public static bool EsCliente(this ISession session)
        {
            return string.Equals(session.GetString(SessionKeys.Rol), "Cliente", StringComparison.OrdinalIgnoreCase);
        }
    }
}
