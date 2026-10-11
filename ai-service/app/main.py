import os
from typing import Optional
from fastapi import FastAPI, UploadFile, File, Form, HTTPException, status
from fastapi.middleware.cors import CORSMiddleware
from app.schemas import InvoiceExtractResponse
from app.services.ocr_service import extract_invoice_with_gemini

app = FastAPI(
    title="WMS AI & Optimization Service",
    version="1.0.0",
    description="Microservice xử lý AI OCR và các thuật toán tối ưu hóa vận hành kho WMS.",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.get("/health", status_code=status.HTTP_200_OK)
async def health_check():
    return {"status": "ok", "service": "wms-ai-service"}


@app.post(
    "/api/v1/invoices/extract",
    response_model=InvoiceExtractResponse,
    summary="Bóc tách thông tin hóa đơn / phiếu nhận kho từ ảnh hoặc file PDF",
)
async def extract_invoice(
    file: UploadFile = File(..., description="File ảnh chứng từ (JPG, PNG, WEBP) hoặc PDF"),
    api_key: Optional[str] = Form(None, description="Google Gemini API Key (tùy chọn, fallback sang ENV)"),
    model: Optional[str] = Form(None, description="Tên mô hình Gemini (mặc định gemini-1.5-flash)"),
):
    key_to_use = api_key or os.getenv("GEMINI_API_KEY", "")
    if not key_to_use:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Thiếu Gemini API Key. Vui lòng truyền trong form 'api_key' hoặc cấu hình biến môi trường GEMINI_API_KEY.",
        )

    try:
        content = await file.read()
        if not content:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="File tải lên rỗng.",
            )

        result = await extract_invoice_with_gemini(
            file_bytes=content,
            filename=file.filename or "invoice.jpg",
            api_key=key_to_use,
            model=model or os.getenv("GEMINI_MODEL", "gemini-1.5-flash"),
        )
        return result
    except ValueError as err:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail=str(err)) from err
    except RuntimeError as err:
        raise HTTPException(status_code=status.HTTP_502_BAD_GATEWAY, detail=str(err)) from err
    except Exception as err:
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail=f"Lỗi xử lý file: {str(err)}") from err
