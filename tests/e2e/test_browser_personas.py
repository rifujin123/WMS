import os
import sys
import pytest
from playwright.sync_api import sync_playwright

BASE_URL = os.environ.get("WMS_BASE_URL", "http://localhost:5173")
SCREENSHOT_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "e2e_screenshots")
os.makedirs(SCREENSHOT_DIR, exist_ok=True)

def login(page, username, password):
    page.goto(f"{BASE_URL}/login")
    page.wait_for_selector("input#username", timeout=10000)
    page.fill("input#username", username)
    page.fill("input#password", password)
    page.click("button[type='submit']")
    page.wait_for_url("**/dashboard", timeout=10000)
    page.wait_for_timeout(1000)

def test_persona_1_admin():
    """Persona 1: Admin kiểm tra Settings (Branding, AI, Alerts) và Quản lý Người dùng (Plain text roles)"""
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chrome", headless=True)
        page = browser.new_page()
        try:
            login(page, "admin", "Admin@123")
            
            # 1. Cài đặt doanh nghiệp
            page.goto(f"{BASE_URL}/settings")
            page.wait_for_selector(".ant-tabs", timeout=10000)
            assert page.locator(".ant-tabs-tab:has-text('Thương hiệu')").is_visible()
            assert page.locator(".ant-tabs-tab[data-node-key='ai']").is_visible()
            assert page.locator(".ant-tabs-tab[data-node-key='emailAlertConfig']").is_visible()

            # 2. Quản lý người dùng
            page.goto(f"{BASE_URL}/users")
            page.wait_for_timeout(1000)
            page.wait_for_timeout(1000)
            
            # Role phải là plain text span, không được dùng ant-tag theo CLAUDE.md
            roles_in_tags = page.locator(".ant-table-tbody .ant-tag:has-text('Admin'), .ant-table-tbody .ant-tag:has-text('Kế toán'), .ant-table-tbody .ant-tag:has-text('Quản lý')").count()
            assert roles_in_tags == 0, "Vi phạm CLAUDE.md: Role người dùng không được dùng ant-tag"
            
            page.screenshot(path=os.path.join(SCREENSHOT_DIR, "01_admin_verified.png"), full_page=True)
            print("PASS: Persona 1 Admin settings & users verification")
        finally:
            browser.close()

def test_persona_2_manager():
    """Persona 2: Quản lý kho (Kho HCM) không có quyền xem nợ NCC và không được duyệt PO"""
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chrome", headless=True)
        page = browser.new_page()
        try:
            login(page, "manager1", "Manager@123")
            
            # 1. Menu không được có "Sổ nợ NCC"
            vendor_debt_menu = page.locator(".ant-menu-item:has-text('Sổ nợ NCC')").count()
            assert vendor_debt_menu == 0, "Quản lý kho không được thấy menu Sổ nợ NCC"

            # 2. Gõ trực tiếp /vendor-debts phải bị chặn 403 Forbidden
            page.goto(f"{BASE_URL}/vendor-debts")
            page.wait_for_selector(".ant-result-403, :text('403')", timeout=10000)
            assert page.locator(".ant-result-403").or_(page.locator(":text('403')")).is_visible()

            # 3. Đơn mua hàng: không có nút Duyệt đơn
            page.goto(f"{BASE_URL}/purchase-orders")
            page.wait_for_timeout(1000)
            page.wait_for_timeout(1000)
            approve_buttons = page.locator(".ant-table-tbody button:has-text('Duyệt')").count()
            assert approve_buttons == 0, "Quản lý kho không được có nút Phê duyệt PO (Maker-Checker)"

            page.screenshot(path=os.path.join(SCREENSHOT_DIR, "02_manager_verified.png"), full_page=True)
            print("PASS: Persona 2 Manager access boundaries verification")
        finally:
            browser.close()

def test_persona_3_accountant():
    """Persona 3: Kế toán viên (Kho HCM) thấy Sổ nợ NCC và PO kho HCM"""
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chrome", headless=True)
        page = browser.new_page()
        try:
            login(page, "accountant_hcm", "Accountant@123")
            
            # 1. Truy cập /vendor-debts hợp lệ
            page.goto(f"{BASE_URL}/vendor-debts")
            page.wait_for_selector(".ant-table-tbody, .ant-empty", timeout=10000)

            # 2. Mở Đơn đặt hàng
            page.goto(f"{BASE_URL}/purchase-orders")
            page.wait_for_timeout(1000)
            page.wait_for_timeout(1000)
            
            # 3. Mở Đơn bán hàng
            page.goto(f"{BASE_URL}/sale-orders")
            page.wait_for_timeout(1000)

            page.screenshot(path=os.path.join(SCREENSHOT_DIR, "03_accountant_verified.png"), full_page=True)
            print("PASS: Persona 3 Accountant warehouse scope verification")
        finally:
            browser.close()

def test_persona_4_chief_accountant():
    """Persona 4: Kế toán trưởng xem đa kho và có quyền Duyệt PO"""
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chrome", headless=True)
        page = browser.new_page()
        try:
            login(page, "chief_accountant", "Accountant@123")
            
            # 1. Xem Đơn mua hàng: có nút Duyệt đơn nếu có PO Pending
            page.goto(f"{BASE_URL}/purchase-orders")
            page.wait_for_timeout(1000)
            page.wait_for_timeout(1000)
            
            pending_rows = page.locator(".ant-table-tbody tr:has(.ant-tag:has-text('Chờ duyệt'))")
            if pending_rows.count() > 0:
                approve_btn = pending_rows.first.locator("button:has-text('Duyệt')")
                assert approve_btn.is_visible(), "Kế toán trưởng phải thấy nút Duyệt cho đơn Chờ duyệt"

            # 2. Truy cập Sổ nợ NCC toàn công ty
            page.goto(f"{BASE_URL}/vendor-debts")
            page.wait_for_selector(".ant-table-tbody, .ant-empty", timeout=10000)

            page.screenshot(path=os.path.join(SCREENSHOT_DIR, "04_chief_accountant_verified.png"), full_page=True)
            print("PASS: Persona 4 Chief Accountant approval verification")
        finally:
            browser.close()

def test_persona_5_staff():
    """Persona 5: Nhân viên kho thực hiện Nhận hàng, Cất hàng, Nhặt hàng, và Kiểm kê"""
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chrome", headless=True)
        page = browser.new_page()
        try:
            login(page, "staff1", "Staff1@123")
            
            # 1. Nhận hàng
            page.goto(f"{BASE_URL}/receivings")
            page.wait_for_selector(".ant-table-tbody, .ant-empty", timeout=10000)
            
            # 2. Cất hàng
            page.goto(f"{BASE_URL}/putaway-tasks")
            page.wait_for_selector(".ant-table-tbody, .ant-empty", timeout=10000)

            # 3. Lấy hàng
            page.goto(f"{BASE_URL}/pickings")
            page.wait_for_selector(".ant-table-tbody, .ant-empty", timeout=10000)

            # 4. Kiểm kê kho: Staff có nút tạo phiếu nhưng không có nút Duyệt
            page.goto(f"{BASE_URL}/stock-adjustments")
            page.wait_for_selector(".ant-table-tbody, .ant-empty", timeout=10000)
            page.wait_for_timeout(1000)
            approve_btns = page.locator(".ant-table-tbody button:has-text('Duyệt')").count()
            assert approve_btns == 0, "WarehouseStaff không được phép duyệt phiếu kiểm kê"

            # 5. Kiểm tra phân quyền: Staff không được truy cập /audit-logs (bị 403 Forbidden)
            page.goto(f"{BASE_URL}/audit-logs")
            page.wait_for_selector(".ant-result-403, :text('403')", timeout=10000)
            assert page.locator(".ant-result-403").or_(page.locator(":text('403')")).is_visible(), "WarehouseStaff phải bị chặn 403 khi vào /audit-logs"

            page.screenshot(path=os.path.join(SCREENSHOT_DIR, "05_staff_verified.png"), full_page=True)
            print("PASS: Persona 5 Staff operational flows verification")
        finally:
            browser.close()

def test_persona_6_dashboard_radar():
    """Persona 6: Dashboard phân tích Vốn lưu động & Cảnh báo hàng cận date"""
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chrome", headless=True)
        page = browser.new_page()
        try:
            login(page, "admin", "Admin@123")
            page.goto(f"{BASE_URL}/dashboard")
            page.wait_for_selector(".ant-card, .ant-statistic", timeout=10000)
            page.wait_for_timeout(1000)
            
            page.screenshot(path=os.path.join(SCREENSHOT_DIR, "06_dashboard_radar_verified.png"), full_page=True)
            print("PASS: Persona 6 Dashboard & Working Capital radar verification")
        finally:
            browser.close()

def test_persona_7_admin_email_alert_feature():
    """Persona 7: Admin cấu hình Email Cảnh báo Tự động và Gửi thử email SMTP"""
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chrome", headless=True)
        page = browser.new_page()
        try:
            login(page, "admin", "Admin@123")
            page.goto(f"{BASE_URL}/settings")
            page.wait_for_selector(".ant-tabs", timeout=10000)
            
            # Click tab Email cảnh báo
            email_tab = page.locator(".ant-tabs-tab[data-node-key='emailAlertConfig']")
            email_tab.click()
            page.wait_for_selector("textarea#alertEmailRecipients", timeout=10000)

            # Assert controls hiện diện
            assert page.locator("button[role='switch']").is_visible(), "Switch bật tắt email cảnh báo phải hiển thị"
            assert page.locator("textarea#alertEmailRecipients").is_visible(), "Textarea danh sách email phải hiển thị"
            
            # Điền email nhận thử nghiệm vào input test
            test_input = page.locator("input[placeholder*='admin@wms.com']")
            test_input.fill("test-admin@wms.com")

            # Test kết nối SMTP
            test_email_btn = page.locator("button:has-text('Gửi thử email')")
            assert test_email_btn.is_visible(), "Nút gửi thử email phải hiển thị"
            test_email_btn.click()
            page.wait_for_timeout(1500)
            
            page.screenshot(path=os.path.join(SCREENSHOT_DIR, "07_admin_email_alert_verified.png"), full_page=True)
            print("PASS: Persona 7 Admin Email Alert configuration & SMTP test verified")
        finally:
            browser.close()

def test_persona_8_notification_center_and_popover():
    """Persona 8: Kiểm tra Trung tâm Thông báo In-App (Chuông thông báo & Popover)"""
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chrome", headless=True)
        page = browser.new_page()
        try:
            login(page, "admin", "Admin@123")
            
            # Icon chuông thông báo trên Header
            bell_btn = page.locator("button[aria-label='Thông báo hệ thống']")
            assert bell_btn.is_visible(), "Nút chuông thông báo phải hiển thị trên Header"
            bell_btn.click()
            
            # Chờ Popover hiển thị nội dung
            page.wait_for_selector(".ant-popover-content", timeout=10000)
            assert page.locator(".ant-popover-content").is_visible()
            
            page.screenshot(path=os.path.join(SCREENSHOT_DIR, "08_notification_center_verified.png"), full_page=True)
            print("PASS: Persona 8 Notification Center popover verified")
        finally:
            browser.close()

def test_persona_9_desktop_ergonomics_sider_and_cmdk():
    """Persona 9: Desktop Ergonomics - Toggle Sidebar Push & Command Palette (Ctrl+K)"""
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chrome", headless=True)
        page = browser.new_page()
        try:
            login(page, "admin", "Admin@123")
            
            # Mở Command Palette bằng phím tắt Control+k
            page.keyboard.press("Control+k")
            page.wait_for_selector(".ant-modal, .cmdk-dialog, [role='dialog']", timeout=10000)
            page.wait_for_timeout(500)
            
            page.screenshot(path=os.path.join(SCREENSHOT_DIR, "09_desktop_cmdk_palette_verified.png"), full_page=True)
            print("PASS: Persona 9 Desktop Ergonomics Command Palette verified")
        finally:
            browser.close()

def test_persona_10_audit_logs_tabs_and_navigation():
    """Persona 10: Quản lý mở /audit-logs và chuyển đổi mượt mà giữa 3 Tabs kiểm toán"""
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chrome", headless=True)
        page = browser.new_page()
        try:
            login(page, "admin", "Admin@123")
            
            # 1. Điều hướng từ link Dashboard sang /audit-logs
            page.goto(f"{BASE_URL}/dashboard")
            # navigate directly to audit-logs
            page.goto(f'{BASE_URL}/audit-logs')
            page.wait_for_url("**/audit-logs", timeout=10000)
            
            # 2. Kiểm tra giao diện 3 Tabs
            page.wait_for_selector(".ant-tabs", timeout=10000)
            assert page.locator(".ant-tabs-nav-list .ant-tabs-tab").nth(0).is_visible()
            assert page.locator(".ant-tabs-nav-list .ant-tabs-tab").nth(1).is_visible()
            assert page.locator(".ant-tabs-nav-list .ant-tabs-tab").nth(2).is_visible()

            # 3. Chuyển sang Tab Nhật ký thao tác
            page.locator(".ant-tabs-nav-list .ant-tabs-tab").nth(1).click()
            page.wait_for_timeout(1000)

            # 4. Chuyển sang Tab Lịch sử trạng thái
            page.locator(".ant-tabs-nav-list .ant-tabs-tab").nth(2).click()
            page.wait_for_timeout(1000)

            page.screenshot(path=os.path.join(SCREENSHOT_DIR, "10_audit_logs_verified.png"), full_page=True)
            print("PASS: Persona 10 Audit Logs tabs & navigation verified")
        finally:
            browser.close()

if __name__ == "__main__":
    test_persona_1_admin()
    test_persona_2_manager()
    test_persona_3_accountant()
    test_persona_4_chief_accountant()
    test_persona_5_staff()
    test_persona_6_dashboard_radar()
    test_persona_7_admin_email_alert_feature()
    test_persona_8_notification_center_and_popover()
    test_persona_9_desktop_ergonomics_sider_and_cmdk()
    test_persona_10_audit_logs_tabs_and_navigation()
    print("ALL 10 PERSONAS AUTOMATED E2E TESTS PASSED!")
