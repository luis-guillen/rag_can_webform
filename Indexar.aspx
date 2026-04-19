<%@ Page Title="Indexar Corpus" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Indexar.aspx.cs" Inherits="rag_can_aspx.Indexar" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="container">
        <h1 class="mb-4">
            <i class="fas fa-tags"></i> Indexar Corpus
        </h1>
        <p class="lead">
            Genera o actualiza <code>metadata.json</code> a partir de los archivos <code>.txt</code>
            ya crawleados, sin volver a crawlear.
        </p>

        <div class="card mb-4">
            <div class="card-body">

                <div class="mb-3">
                    <label for="ddlCarpeta" class="form-label">
                        <i class="fas fa-folder-open"></i> Carpeta bajo <code>App_Data/</code>:
                    </label>
                    <asp:DropDownList ID="ddlCarpeta" runat="server" CssClass="form-select" style="max-width:320px;">
                    </asp:DropDownList>
                    <small class="form-text text-muted d-block mt-1">
                        Subcarpetas detectadas automáticamente en <code>App_Data/</code>.
                    </small>
                </div>

                <div class="mb-3">
                    <label for="txtCarpetaCustom" class="form-label">
                        <i class="fas fa-keyboard"></i> O introduce una ruta personalizada (relativa a <code>App_Data/</code>):
                    </label>
                    <asp:TextBox ID="txtCarpetaCustom" runat="server" CssClass="form-control"
                        Placeholder="ej: crawlings/2026-04-18_run3" style="max-width:400px;" />
                    <small class="form-text text-muted d-block mt-1">
                        Deja vacío para usar la selección del dropdown.
                    </small>
                </div>

                <div class="mb-3 form-check">
                    <asp:CheckBox ID="chkRecursivo" runat="server" CssClass="form-check-input" Checked="true" />
                    <label class="form-check-label" for="chkRecursivo">
                        Escanear subdirectorios recursivamente
                        <small class="text-muted">(necesario si la carpeta contiene subcarpetas por dominio)</small>
                    </label>
                </div>

                <asp:Label ID="lblError" runat="server" CssClass="alert alert-danger d-block mb-3"
                    Visible="false"></asp:Label>

                <asp:Button ID="btnIndexar" runat="server" Text="Escanear y generar metadatos"
                    CssClass="btn btn-primary btn-lg" OnClick="BtnIndexar_Click" />
            </div>
        </div>

        <asp:PlaceHolder ID="phResumen" runat="server" Visible="false">
            <div class="card">
                <div class="card-body">
                    <h4 class="mb-3"><i class="fas fa-chart-bar"></i> Resultado</h4>
                    <asp:Literal ID="litResumen" runat="server" Mode="PassThrough"></asp:Literal>
                </div>
            </div>
        </asp:PlaceHolder>
    </div>
</asp:Content>
