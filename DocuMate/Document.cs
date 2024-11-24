namespace CommUnity_Hub
{
    public class Document
    {
        public int DocumentID { get; set; }
        public required string DocumentName { get; set; }
        public required string DocumentType { get; set; }
        public DateTime DocumentDate { get; set; }
    }
}
