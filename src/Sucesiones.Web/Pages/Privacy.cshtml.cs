using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Sucesiones.Web.Pages;

public class PrivacyModel : PageModel
{
    private readonly ILogger<PrivacyModel> _registroEventos;

    public PrivacyModel(ILogger<PrivacyModel> registroEventos)
    {
        _registroEventos = registroEventos;
    }

    public void OnGet()
    {
    }
}
