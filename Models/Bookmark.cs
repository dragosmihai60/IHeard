using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace IHeard.Models
{
    public class Bookmark
    {
        [Key]
        public int BookmarkId { get; set; }

        [Required(ErrorMessage = "Numele colectiei este obligatoriu")]
        public string BookmarkName { get; set; }
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }
        public virtual ICollection<NewsBookmark>? NewsBookmarks { get; set; }

    }
}
