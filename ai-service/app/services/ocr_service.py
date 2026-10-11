import base64
import json
import os
from typing import Optional
import httpx
from app.schemas import InvoiceExtractResponse, InvoiceLineItem

SYSTEM_PROMPT = (
    "Bạn là trợ lý AI chuyên gia trích xuất dữ liệu hóa đơn giao hàng / phiếu nhận hàng kho. "
    "Trả về DUY NHẤT một chuỗi JSON hợp lệ, không giải thích thêm, không bọc markdown. "
    "Schema bắt buộc: {"
    "\"invoice_number\": \"\","
    "\"vendor_name\": \"\","
    "\"invoice_date\": \"yyyy-MM-ddTHH:mm:ss hoặc null\","
    "\"line_items\": ["
    "  {\"sku\": \"\", \"name\": \"\", \"quantity\": 0, \"unit_price\": 0, \"total_amount\": 0}"
    "]"
    "}. "
    "sku là mã hàng nếu có, nếu không có để rỗng. "
    "unit_price và total_amount là số nguyên hoặc số thực nếu có, nếu không thấy thì để null hoặc 0."
)

MIME_MAP = {
    ".png": "image/png",
    ".webp": "image/webp",
    ".heic": "image/heic",
    ".heif": "image/heif",
    ".pdf": "application/pdf",
    ".jpg": "image/jpeg",
    ".jpeg": "image/jpeg",
}


def get_mime_type(filename: str) -> str:
    ext = os.path.splitext(filename)[1].lower()
    return MIME_MAP.get(ext, "image/jpeg")


def calculate_confidence(response: InvoiceExtractResponse) -> float:
    # Điểm đánh giá chất lượng trích xuất tài liệu (0.0 - 1.0)
    score = 0.5
    if response.invoice_number.strip():
        score += 0.2
    if response.vendor_name.strip():
        score += 0.1
    if response.line_items:
        score += 0.2
        # Kiểm tra tỷ lệ có sku hoặc name
        valid_items = sum(1 for item in response.line_items if item.name.strip() and item.quantity > 0)
        if valid_items == len(response.line_items):
            score = min(1.0, score)
    return round(score, 2)


async def extract_invoice_with_gemini(
    file_bytes: bytes,
    filename: str,
    api_key: str,
    model: Optional[str] = None,
    base_url: str = "https://generativelanguage.googleapis.com",
) -> InvoiceExtractResponse:
    if not api_key:
        raise ValueError("Google Gemini API Key không được để trống.")

    model_name = model or "gemini-1.5-flash"
    mime_type = get_mime_type(filename)
    b64_data = base64.b64encode(file_bytes).decode("utf-8")

    payload = {
        "contents": [
            {
                "role": "user",
                "parts": [
                    {"text": "Hãy trích xuất chi tiết dữ liệu từ chứng từ hóa đơn này."},
                    {"inlineData": {"mimeType": mime_type, "data": b64_data}},
                ],
            }
        ],
        "systemInstruction": {"parts": [{"text": SYSTEM_PROMPT}]},
        "generationConfig": {
            "responseMimeType": "application/json",
            "temperature": 0.1,
        },
    }

    url = f"{base_url.rstrip('/')}/v1beta/models/{model_name}:generateContent"
    headers = {"x-goog-api-key": api_key, "Content-Type": "application/json"}

    async with httpx.AsyncClient(timeout=60.0) as client:
        res = await client.post(url, json=payload, headers=headers)
        if res.status_code != 200:
            raise RuntimeError(f"Gemini API ({model_name}) lỗi [{res.status_code}]: {res.text}")

        data = res.json()
        try:
            raw_text = data["candidates"][0]["content"]["parts"][0]["text"]
        except (KeyError, IndexError) as err:
            raise RuntimeError(f"Gemini response không chứa nội dung: {data}") from err

    # Loại bỏ code block markdown nếu model vẫn trả về
    clean_text = raw_text.strip()
    if clean_text.startswith("```json"):
        clean_text = clean_text[7:]
    elif clean_text.startswith("```"):
        clean_text = clean_text[3:]
    if clean_text.endswith("```"):
        clean_text = clean_text[:-3]
    clean_text = clean_text.strip()

    parsed = json.loads(clean_text)

    line_items = [
        InvoiceLineItem(
            sku=item.get("sku") or "",
            name=item.get("name") or "",
            quantity=int(item.get("quantity") or 0),
            unit_price=float(item.get("unit_price")) if item.get("unit_price") is not None else None,
            total_amount=float(item.get("total_amount")) if item.get("total_amount") is not None else None,
        )
        for item in parsed.get("line_items", [])
    ]

    result = InvoiceExtractResponse(
        invoice_number=parsed.get("invoice_number") or "",
        vendor_name=parsed.get("vendor_name") or "",
        invoice_date=parsed.get("invoice_date"),
        line_items=line_items,
    )
    result.confidence_score = calculate_confidence(result)
    return result
