<%@ Page Title="Indexar Corpus" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Indexar.aspx.cs" Inherits="rag_can_aspx.Indexar" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="container">
        <h1 class="mb-4">
            <i class="fas fa-tags"></i> Indexar Corpus
        </h1>
        <p class="lead">
            Indexado incremental en segundo plano: solo procesa lo que el crawler marco como
            <code>needs_index = true</code>. El estado se persiste en <code>App_Data/status</code>.
        </p>

        <!-- Indexado incremental en background -->
        <div class="card mb-4">
            <div class="card-body">
                <h4 class="mb-3"><i class="fas fa-bolt"></i> Indexado incremental (background)</h4>

                <asp:UpdatePanel ID="updIndexControl" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <asp:Label ID="lblIndexMsg" runat="server" CssClass="alert alert-info d-block mb-3" Visible="false"></asp:Label>
                        <asp:Button ID="btnIniciarIndex" runat="server" Text="Iniciar Indexado"
                            CssClass="btn btn-primary btn-lg" OnClick="BtnIniciarIndex_Click" />
                        <asp:Button ID="btnPararIndex" runat="server" Text="Parar"
                            CssClass="btn btn-outline-danger btn-lg ms-2" OnClick="BtnPararIndex_Click" />
                    </ContentTemplate>
                </asp:UpdatePanel>

                <asp:UpdatePanel ID="updIndexEstado" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <asp:Timer ID="tmrIndex" runat="server" Interval="3000" OnTick="TmrIndex_Tick" />
                        <div class="mt-3">
                            <asp:Literal ID="litIndexEstado" runat="server" Mode="PassThrough"></asp:Literal>
                        </div>
                        <div class="mt-3">
                            <h5><i class="fas fa-terminal"></i> Logs recientes (indexer.log)</h5>
                            <asp:Literal ID="litIndexLogs" runat="server" Mode="PassThrough"></asp:Literal>
                        </div>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </div>
        </div>

        <!-- Vectorizar corpus en Qdrant (Python pipeline) -->
        <div class="card mb-4">
            <div class="card-body">
                <h4 class="mb-3"><i class="fas fa-database"></i> Vectorizar corpus en Qdrant</h4>
                <p class="text-muted">
                    Ejecuta <code>app.chunk --full</code> seguido de <code>app.embed_index</code>:
                    re-genera todos los fragmentos del corpus y los sube a la colección <code>rag_canarias</code> en Qdrant.
                    Requiere que la <strong>Demo API</strong> (Qdrant en Docker) esté activa.
                </p>

                <asp:UpdatePanel ID="updVectorizarControl" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <asp:Label ID="lblVectorizarMsg" runat="server" CssClass="alert d-block mb-3" Visible="false"></asp:Label>
                        <asp:Button ID="btnVectorizar" runat="server" Text="Chunk + Vectorizar en Qdrant"
                            CssClass="btn btn-success btn-lg" OnClick="BtnVectorizar_Click" />
                    </ContentTemplate>
                </asp:UpdatePanel>

                <asp:UpdatePanel ID="updVectorizarEstado" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <asp:Timer ID="tmrVectorizar" runat="server" Interval="3000" Enabled="false" OnTick="TmrVectorizar_Tick" />
                        <asp:PlaceHolder ID="phVectorizarProgress" runat="server" Visible="false"></asp:PlaceHolder>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </div>
        </div>

        <!-- Escaneo manual de metadata (herramienta de reparacion) -->
        <div class="card mb-4">
            <div class="card-body">
                <h4 class="mb-3"><i class="fas fa-folder-open"></i> Reparar sidecars (mantenimiento)</h4>
                <p class="text-muted">
                    <strong>No es parte del flujo normal.</strong>
                    Usa esto solo si los archivos <code>*.metadata.json</code> se corrompieron, se borraron,
                    o si tienes carpetas de <code>.txt</code> antiguas sin sidecars.
                    Regenera los sidecars y el <code>metadata.json</code> raíz a partir de los <code>.txt</code>
                    ya descargados, detecta duplicados entre páginas y limpia BOM.
                    El crawler nuevo genera los sidecars correctamente de forma automática.
                </p>

                <div class="mb-3">
                    <label for="ddlCarpeta" class="form-label">
                        Carpeta bajo <code>App_Data/</code>:
                    </label>
                    <asp:DropDownList ID="ddlCarpeta" runat="server" CssClass="form-select" style="max-width:320px;">
                    </asp:DropDownList>
                </div>

                <div class="mb-3">
                    <label for="txtCarpetaCustom" class="form-label">
                        O ruta personalizada (relativa a <code>App_Data/</code>):
                    </label>
                    <asp:TextBox ID="txtCarpetaCustom" runat="server" CssClass="form-control"
                        Placeholder="ej: crawlings/elmuseocanario_com" style="max-width:400px;" />
                </div>

                <div class="mb-3 form-check">
                    <asp:CheckBox ID="chkRecursivo" runat="server" CssClass="form-check-input" Checked="true" />
                    <label class="form-check-label" for="chkRecursivo">
                        Escanear subdirectorios recursivamente
                    </label>
                </div>

                <asp:Label ID="lblError" runat="server" CssClass="alert alert-danger d-block mb-3"
                    Visible="false"></asp:Label>

                <asp:Button ID="btnIndexar" runat="server" Text="Reparar sidecars"
                    CssClass="btn btn-secondary" OnClick="BtnIndexar_Click" />
            </div>
        </div>

        <asp:PlaceHolder ID="phResumen" runat="server" Visible="false">
            <div class="card">
                <div class="card-body">
                    <h4 class="mb-3"><i class="fas fa-chart-bar"></i> Resultado del escaneo manual</h4>
                    <asp:Literal ID="litResumen" runat="server" Mode="PassThrough"></asp:Literal>
                </div>
            </div>
        </asp:PlaceHolder>
    </div>
</asp:Content>
