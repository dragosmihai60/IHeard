using System.ComponentModel.DataAnnotations;

namespace IHeard.Models
{
    public class Comment
    {
        [Key]
        public int CommentId { get; set; }

        [Required(ErrorMessage = "Continutul comentariului este obligatoriu")]
        public string CommentContent { get; set; }

        public DateTime CommentDate { get; set; }

        public int? NewsId { get; set; }
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; } //un comm apartine unui singur utiliazator

        public virtual News? News { get; set; }
    }

}
