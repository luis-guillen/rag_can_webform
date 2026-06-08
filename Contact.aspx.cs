using System;
using System.Web.UI;

namespace rag_can_aspx
{
    public partial class Contact : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Redirect("~/Landing.aspx", true);
        }
    }
}
