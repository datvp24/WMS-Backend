using System.Text.RegularExpressions;


namespace Wms.Application.Services.Warehouses { 
    public static class LocationCodeValidator
    {
        // Accept patterns like A1-01-03 or B12-10-99
        private static readonly Regex _regex = new Regex("^[A-Z][0-9]{1,2}-[0-9]{2}-[0-9]{2}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);


        public static bool IsValid(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            code = code.Trim();
            return _regex.IsMatch(code);
        }
    }
}