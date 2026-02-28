using System.ComponentModel.DataAnnotations;

namespace LaptopStore.DTOs.CategoryDTOs
{
    public class AddCategoryRequest
    {
        [Required(ErrorMessage = "Tên phân loại không được để trống.")]
        [StringLength(100)]
        public string Name { get; set; }

        public string? Description { get; set; }
    }
}
