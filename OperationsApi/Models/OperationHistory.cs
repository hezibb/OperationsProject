namespace OperationsApi.Models
{
   
    public class OperationHistory
    {
        public int Id { get; set; } 

        public string Field1 { get; set; } = string.Empty; 
        public string Field2 { get; set; } = string.Empty; 
        public string Operation { get; set; } = string.Empty; 
        public string Result { get; set; } = string.Empty; 

        public DateTime ExecutedAt { get; set; } 
    }
}