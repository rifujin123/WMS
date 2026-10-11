import json
import pytest
from httpx import ASGITransport, AsyncClient
from unittest.mock import patch
from app.main import app
from app.schemas import InvoiceExtractResponse, InvoiceLineItem


@pytest.mark.asyncio
async def test_health_check():
    transport = ASGITransport(app=app)
    async with AsyncClient(transport=transport, base_url="http://test") as client:
        response = await client.get("/health")
        assert response.status_code == 200
        data = response.json()
        assert data["status"] == "ok"
        assert data["service"] == "wms-ai-service"


@pytest.mark.asyncio
async def test_extract_invoice_missing_api_key():
    transport = ASGITransport(app=app)
    async with AsyncClient(transport=transport, base_url="http://test") as client:
        files = {"file": ("test.png", b"fake-image-bytes", "image/png")}
        response = await client.post("/api/v1/invoices/extract", files=files)
        assert response.status_code == 400
        assert "Gemini API Key" in response.json()["detail"]


@pytest.mark.asyncio
async def test_extract_invoice_success_mock():
    mock_extracted = InvoiceExtractResponse(
        invoice_number="HD-2026-TEST",
        vendor_name="Cong ty TNHH Demo",
        invoice_date="2026-10-11T00:00:00",
        line_items=[
            InvoiceLineItem(
                sku="SKU-TEST-01",
                name="San pham Test 01",
                quantity=15,
                unit_price=100000.0,
                total_amount=1500000.0,
            )
        ],
        confidence_score=1.0,
    )

    with patch("app.main.extract_invoice_with_gemini", return_value=mock_extracted):
        transport = ASGITransport(app=app)
        async with AsyncClient(transport=transport, base_url="http://test") as client:
            files = {"file": ("test.png", b"fake-bytes", "image/png")}
            data = {"api_key": "test-key-123"}
            response = await client.post("/api/v1/invoices/extract", files=files, data=data)
            assert response.status_code == 200
            res_json = response.json()
            assert res_json["invoice_number"] == "HD-2026-TEST"
            assert res_json["vendor_name"] == "Cong ty TNHH Demo"
            assert len(res_json["line_items"]) == 1
            assert res_json["line_items"][0]["sku"] == "SKU-TEST-01"
            assert res_json["line_items"][0]["quantity"] == 15
            assert res_json["confidence_score"] == 1.0
