using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace CleanArq.Application.DTOs
{
    public class ProductDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "The Name is required")]
        [MaxLength(100, ErrorMessage = "The Name must have a maximum length of 100 characters")]
        [MinLength(3, ErrorMessage = "The Name must have a minimum length of 3 characters")]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "The Description is required")]
        [MaxLength(255, ErrorMessage = "The Description must have a maximum length of 255 characters")]
        [MinLength(5, ErrorMessage = "The Description must have a minimum length of 5 characters")]
        [Display(Name = "Product Description")]
        public string Description { get; set; } = string.Empty;

        
        [Required(ErrorMessage = "The Price is required")]
        [Column(TypeName = "decimal(18,2)")]
        [DisplayFormat(DataFormatString = "{0:C2}")]
        [Display(Name = "Product Price")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "The Stock is required")]
        [Range(1, 10000, ErrorMessage = "The Stock must be a non-negative integer")]
        [Display(Name = "Product Stock")]
        public int Stock { get; set; }
        
        [Display(Name = "Product Image URL")]
        [MaxLength(255, ErrorMessage = "The Image URL must have a maximum length of 255 characters")]
        public string ImageUrl { get; set; } = string.Empty;

        [Display(Name = "Category")]
        public CategoryDTO? Category { get; set; }
        public int CategoryId { get; set; }
    }
}
