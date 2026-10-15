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
    """Persona 1: Admin kiểm tra Settings, Quản lý User và Layout Sider mới"""
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chrome", headless=True)
        page = browser.new_page(viewport={"width": 1440, "height": 900})
        try:
            login(page, "admin", "Admin@123")
            
            # Chụp ảnh Dashboard với Sider Push mới & Header Toggle
            page.screenshot(path=os.path.join(SCREENSHOT_DIR, "01_layout_admin_dashboard_expanded.png"), full_page=True)

            # Bấm thu nhỏ Sidebar sang Mini-icon 64px
            toggle_btn = page.locator("header button[aria-label*='sidebar']")
            if toggle_btn.is_visible():
                toggle_btn.click()
                page.wait_for_timeout(500)
                page.screenshot(path=os.path.join(SCREENSHOT_DIR, "01_layout_admin_dashboard_collapsed_mini.png"), full_page=True)
                # Bấm mở lại
                toggle_btn.click()
                page.wait_for_timeout(500)

            # Vào Quản lý người dùng
            page.goto(f"{BASE_URL}/users")
            page.wait_for_selector(".ant-table-tbody", timeout=10000)
            page.screenshot(path=os.path.join(SCREENSHOT_DIR, "01_admin_users.png"), full_page=True)
        finally:
            browser.close()

def test_persona_2_manager():
    """Persona 2: Quản lý kho xem Đơn mua hàng PO (không bị che nút Tạo PO và cột bảng)"""
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chrome", headless=True)
        page = browser.new_page(viewport={"width": 1440, "height": 900})
        try:
            login(page, "manager1", "Manager@123")
            page.goto(f"{BASE_URL}/purchase-orders")
            page.wait_for_selector(".ant-table-tbody", timeout=10000)
            page.wait_for_timeout(1000)
            
            # Chụp màn hình rõ nét PO - thấy đầy đủ nút [Tạo PO] và cột bảng không bị che
            page.screenshot(path=os.path.join(SCREENSHOT_DIR, "02_manager_purchase_orders_no_overlap.png"), full_page=True)
        finally:
            browser.close()

def test_persona_3_accountant():
    """Persona 3: Kế toán viên xem Đơn bán hàng SO và Chuông thông báo"""
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chrome", headless=True)
        page = browser.new_page(viewport={"width": 1440, "height": 900})
        try:
            login(page, "accountant_hcm", "Accountant@123")
            page.goto(f"{BASE_URL}/sale-orders")
            page.wait_for_selector(".ant-table-tbody", timeout=10000)
            page.wait_for_timeout(1000)
            
            # Bấm mở Popover Chuông thông báo trên Header
            bell_btn = page.locator("header button[aria-label='Thông báo hệ thống']")
            if bell_btn.is_visible():
                bell_btn.click()
                page.wait_for_timeout(600)
            
            page.screenshot(path=os.path.join(SCREENSHOT_DIR, "03_accountant_saleorders_with_bell.png"), full_page=True)
        finally:
            browser.close()

def test_persona_4_chief_accountant():
    """Persona 4: Kế toán trưởng duyệt PO và Sổ nợ NCC"""
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chrome", headless=True)
        page = browser.new_page(viewport={"width": 1440, "height": 900})
        try:
            login(page, "chief_accountant", "Accountant@123")
            page.goto(f"{BASE_URL}/purchase-orders")
            page.wait_for_selector(".ant-table-tbody", timeout=10000)
            page.wait_for_timeout(1000)
            page.screenshot(path=os.path.join(SCREENSHOT_DIR, "04_chief_accountant_pos.png"), full_page=True)
        finally:
            browser.close()

def test_persona_5_staff():
    """Persona 5: Nhân viên kho thực hiện Nhận hàng và Cất hàng"""
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chrome", headless=True)
        page = browser.new_page(viewport={"width": 1440, "height": 900})
        try:
            login(page, "staff1", "Staff1@123")
            page.goto(f"{BASE_URL}/receivings")
            page.wait_for_selector(".ant-table-tbody, .ant-empty", timeout=10000)
            page.wait_for_timeout(1000)
            page.screenshot(path=os.path.join(SCREENSHOT_DIR, "05_staff_receivings_clean.png"), full_page=True)
        finally:
            browser.close()

def test_persona_6_dashboard_radar():
    """Persona 6: Dashboard Radar Vốn lưu động và Mini-Sider view"""
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chrome", headless=True)
        page = browser.new_page(viewport={"width": 1440, "height": 900})
        try:
            login(page, "admin", "Admin@123")
            page.goto(f"{BASE_URL}/dashboard")
            page.wait_for_selector(".ant-card, .ant-statistic", timeout=10000)
            page.wait_for_timeout(1000)

            # Thu nhỏ sidebar để xem Dashboard siêu rộng
            toggle_btn = page.locator("header button[aria-label*='sidebar']")
            if toggle_btn.is_visible():
                toggle_btn.click()
                page.wait_for_timeout(500)

            page.screenshot(path=os.path.join(SCREENSHOT_DIR, "06_dashboard_radar_wide_view.png"), full_page=True)
        finally:
            browser.close()

if __name__ == "__main__":
    test_persona_1_admin()
    test_persona_2_manager()
    test_persona_3_accountant()
    test_persona_4_chief_accountant()
    test_persona_5_staff()
    test_persona_6_dashboard_radar()
    print("ALL FRESH SCREENSHOTS CAPTURED SUCCESSFULLY!")
