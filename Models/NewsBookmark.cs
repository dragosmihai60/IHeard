using System.ComponentModel.DataAnnotations.Schema;

namespace IHeard.Models
{
    public class NewsBookmark
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NewsBookmarkId { get; set; } 

        public int? NewsId { get; set; }
        public int? BookmarkId { get; set; } 

        public virtual News? News { get; set; }   
        public virtual Bookmark? Bookmark { get; set; }   

        public DateTime BookmarkDate { get; set; }
    }
}
 