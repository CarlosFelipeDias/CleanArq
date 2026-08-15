using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CleanArq.Application.DTOs
{
    public class CategoryDTO
    {
        public int Id { get; set; }

        
        [Required(ErrorMessage = "The Name is required")]
        [MaxLength(100, ErrorMessage = "The Name must have a maximum length of 100 characters")]
        [MinLength(3, ErrorMessage = "The Name must have a minimum length of 3 characters")]
        public string Name { get; set; } = string.Empty;
    }
}