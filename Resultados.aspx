<%@ Page Title="Resultados" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Resultados.aspx.cs" Inherits="rag_can_aspx.Resultados" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="container">
        <h1 class="mb-4">
            <i class="fas fa-chart-bar"></i> Resultados del Crawling
        </h1>

        <asp:PlaceHolder ID="phError" runat="server" Visible="false">
            <div class="alert alert-danger alert-dismissible fade show" role="alert">
                <strong>Error encontrado:</strong>
                <div style="margin-top: 10px;">
                    <asp:Literal ID="litError" runat="server" Mode="PassThrough"></asp:Literal>
                </div>
                <hr />
                <p style="margin-bottom: 0;">
                    <strong>Sugerencias:</strong>
                    <ul>
                        <li>Verifica que la URL sea correcta y accesible</li>
                        <li>Comprueba tu conexión a internet</li>
                        <li>Intenta con valores más bajos de maxPages o maxDepth</li>
                        <li>Asegúrate de que el servidor web permite el acceso</li>
                        <li>Revisa los permisos de la carpeta App_Data</li>
                    </ul>
                </p>
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Cerrar"></button>
            </div>
            <a href="Default.aspx" class="btn btn-primary">Volver al formulario</a>
        </asp:PlaceHolder>

        <asp:PlaceHolder ID="phSuccess" runat="server" Visible="false">
            <div class="alert alert-info mb-4">
                <strong><i class="fas fa-folder-open"></i> Carpeta base:</strong>
                <code><asp:Literal ID="litCarpeta" runat="server"></asp:Literal></code>
            </div>

            <h3 class="mt-4 mb-3">Dominios procesados:</h3>
            <ul style="list-style: none; padding: 0; max-height: 500px; overflow-y: auto;">
                <asp:Literal ID="litResultados" runat="server" Mode="PassThrough"></asp:Literal>
            </ul>

            <div class="mt-4">
                <a href="Default.aspx" class="btn btn-primary">Volver al formulario</a>
            </div>
        </asp:PlaceHolder>
    </div>
</asp:Content>
