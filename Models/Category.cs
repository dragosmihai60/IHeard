using System.ComponentModel.DataAnnotations;

namespace IHeard.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Numele categoriei este obligatoriu")]
        public string CategoryName { get; set; }

        public virtual ICollection<News>? Newss { get; set; }
    }

}
