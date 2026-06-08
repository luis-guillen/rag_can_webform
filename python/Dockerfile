# Imagen del servicio FastAPI del RAG para Dell Pro Max (NVIDIA GB10 / DGX Spark).
#
# Base: contenedor NGC de PyTorch de NVIDIA. Es la única vía fiable de tener
# torch + CUDA prehechos para ARM64 (aarch64) + Blackwell (sm_121). Los tags
# >= 25.10 soportan GB10. Cambia NGC_TAG si necesitas otra versión.
ARG NGC_TAG=25.11-py3
FROM nvcr.io/nvidia/pytorch:${NGC_TAG}

ENV PYTHONUNBUFFERED=1 \
    PIP_NO_CACHE_DIR=1 \
    HF_HOME=/models/hf

WORKDIR /app

# Capturamos la versión EXACTA de torch que trae la base en un fichero de
# constraints. Así, al instalar las deps de la app (sentence-transformers
# declara torch como dependencia), pip lo considera ya satisfecho y NO lo
# reemplaza por una rueda sin CUDA.
RUN python -c "import torch; print('torch==' + torch.__version__)" > /tmp/torch-constraint.txt \
    && cat /tmp/torch-constraint.txt

COPY requirements-app.txt .
RUN pip install --no-cache-dir -r requirements-app.txt --constraint /tmp/torch-constraint.txt

# Gate de build: si tras instalar las deps el torch perdió su build CUDA,
# fallamos ruidosamente. (is_available() solo es True en runtime con GPU,
# aquí confirmamos que el import y la build CUDA siguen intactos.)
RUN python -c "import torch; print('torch', torch.__version__, 'cuda build:', torch.version.cuda); assert torch.version.cuda is not None, 'torch perdio la build CUDA'"

COPY app/ ./app/

EXPOSE 8000

# Bind a 0.0.0.0 para ser alcanzable por la VPN. El puerto se publica en compose.
CMD ["uvicorn", "app.api:app", "--host", "0.0.0.0", "--port", "8000"]
