using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebProject.Pages
{
    public class PasswordChangedModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Only show this confirmation after a password change
            if (TempData["PasswordChanged"] is not bool changed ||
                !changed)
            {
                return RedirectToPage("/Login");
            }

            return Page();
        }
    }
}
