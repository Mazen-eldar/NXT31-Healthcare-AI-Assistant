using System.ComponentModel.DataAnnotations;

namespace Base.Shared.DTOs

{
    public class Disable2FaDTO
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;
    }
    
}
