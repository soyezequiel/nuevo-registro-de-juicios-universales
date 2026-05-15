using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Sucesiones.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _registroEventos;

    public IndexModel(ILogger<IndexModel> registroEventos)
    {
        _registroEventos = registroEventos;
    }

    public void OnGet()
    {

    }
}
