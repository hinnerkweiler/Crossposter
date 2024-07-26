namespace MastodonService.Models;

public class ImageUpload
{
    public ByteArrayContent Image { get; set; }
    public string AltText { get; set; }
    public string FileName { get; set; }
}