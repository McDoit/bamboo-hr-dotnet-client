namespace BambooHrClient.Models
{
    public class BambooHrReport<T>
    {
        public string Title { get; set; }
        public BambooHrField[] Fields { get; set; }

        public T[] Employees { get; set; }
    }
}