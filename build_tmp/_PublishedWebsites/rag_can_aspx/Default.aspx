<%@ Page Title="Crawler Web" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="rag_can_aspx._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="container">
        <h1 class="mb-4">Crawler RAG Canarias</h1>
        <p class="lead">Procesa una URL o utiliza las webs semilla definidas en <code>App_Data/seeds.txt</code>.</p>

        <div class="card">
            <div class="card-body">
                <div class="mb-3">
                    <label for="txtUrl" class="form-label">
                        <i class="fas fa-link"></i> URL (opcional):
                    </label>
                    <asp:TextBox ID="txtUrl" runat="server" CssClass="form-control"
                        Placeholder="https://ejemplo.com" />
                    <small class="form-text text-muted d-block mt-2">
                        Si está vacía, se usarán las URLs del archivo de semillas configurado.
                    </small>
                </div>

                <div class="mb-3">
                    <label for="txtCarpeta" class="form-label">
                        <i class="fas fa-folder"></i> Carpeta de guardado (dentro de App_Data):
                    </label>
                    <asp:TextBox ID="txtCarpeta" runat="server" CssClass="form-control"
                        Placeholder="(defecto: App_Data/crawlings/)" />
                    <small class="form-text text-muted d-block mt-2">
                        Deja vacío para usar App_Data/crawlings, o introduce un nombre de subcarpeta.
                    </small>
                </div>

                <div class="row">
                    <div class="col-md-6">
                        <div class="mb-3">
                            <label for="txtMaxPages" class="form-label">Max Páginas:</label>
                            <asp:TextBox ID="txtMaxPages" runat="server" CssClass="form-control"
                                TextMode="Number" Text="50" />
                            <small class="form-text text-muted d-block mt-2">
                                Rango: 1 - 10000
                            </small>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="mb-3">
                            <label for="txtMaxDepth" class="form-label">Max Profundidad:</label>
                            <asp:TextBox ID="txtMaxDepth" runat="server" CssClass="form-control"
                                TextMode="Number" Text="2" />
                            <small class="form-text text-muted d-block mt-2">
                                Rango: 0 - 10
                            </small>
                        </div>
                    </div>
                </div>

                <div class="mb-3">
                    <div class="form-check">
                        <asp:CheckBox ID="chkFullCrawl" runat="server" CssClass="form-check-input" />
                        <label class="form-check-label" for="chkFullCrawl">
                            Full Crawl (permitir hasta 1000 páginas)
                        </label>
                    </div>
                </div>

                <asp:Label ID="lblError" runat="server" CssClass="alert alert-danger"
                    Role="alert" Style="display:none;"></asp:Label>

                <div class="mb-0">
                    <asp:Button ID="btnCrawl" runat="server" Text="Iniciar Crawling"
                        CssClass="btn btn-primary btn-lg" OnClick="BtnCrawl_Click" />
                </div>
            </div>
        </div>
    </div>
</asp:Content>
