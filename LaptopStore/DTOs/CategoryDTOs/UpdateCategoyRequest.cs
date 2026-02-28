using System.ComponentModel.DataAnnotations;

public class UpdateCategoryRequest
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên phân loại không được để trống.")]
    [StringLength(100)]
    public string Name { get; set; }

    public string? Description { get; set; }
}
