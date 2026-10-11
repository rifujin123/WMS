from typing import Optional, List
from pydantic import BaseModel, Field


class InvoiceLineItem(BaseModel):
    sku: str = Field(default="", description="Mã SKU sản phẩm nếu có trên hóa đơn")
    name: str = Field(default="", description="Tên sản phẩm")
    quantity: int = Field(default=0, description="Số lượng nhận")
    unit_price: Optional[float] = Field(default=None, description="Đơn giá")
    total_amount: Optional[float] = Field(default=None, description="Thành tiền")


class InvoiceExtractResponse(BaseModel):
    invoice_number: str = Field(default="", description="Số hóa đơn")
    vendor_name: str = Field(default="", description="Tên nhà cung cấp")
    invoice_date: Optional[str] = Field(default=None, description="Ngày lập hóa đơn ISO 8601")
    line_items: List[InvoiceLineItem] = Field(default_factory=list, description="Danh sách mặt hàng")
    confidence_score: float = Field(default=0.0, description="Độ tin cậy trích xuất (0.0 - 1.0)")
