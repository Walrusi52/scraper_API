namespace scraper_API.Models
{
    public class Results
    {
        public string Debtor_Names { get; set; }
        public string Secured_Party_Names { get; set; }
        public IList<File> Files { get; set; } = new List<File>();
    }
    public class File
    {
        public string File_No { get; set; }
        public string File_Date { get; set; }
        public string Refile_Date { get; set; }
        public string Filing_Type { get; set; }
        public string Pages { get; set; }
        public string Images { get; set; }
    }

    public class SearchParameters
    {
        public string Business_Name { get; set; }
        public string Last_Name { get; set; }
        public string First_Name { get; set; }
        public string Middle_Name { get; set; }
        public string Suffix { get; set; }
        public string City { get; set; }
        public string State { get; set; }
    }
}
