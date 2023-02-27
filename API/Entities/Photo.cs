using System.ComponentModel.DataAnnotations.Schema;

namespace API.Entities;

// We don't need need to add a separate DbSet for Photos as they wii be exclusively used in User entity,
// so we just add a table with overriden name ("Photo") here
[Table("Photos")]
public class Photo
{
    public int Id { get; set; }
    public string Url { get; set; }
    public bool IsMain { get; set; }
    public string PublicId { get; set; }

    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; }
}