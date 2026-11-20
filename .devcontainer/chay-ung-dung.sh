#!/usr/bin/env bash
# Chạy ứng dụng ở cổng 5141 (nếu chưa chạy) và mở cổng cho người ngoài xem.
# Gọi mỗi lần codespace bật lên hoặc mở lại cửa sổ, kể cả sau khi codespace tự tắt vì để lâu không dùng.
cd "$(dirname "$0")/.."

if ! pgrep -f '[Q]uanLyKho.Web' > /dev/null; then
  dotnet build src/QuanLyKho.Web -v q > /tmp/quanlykho-build.log 2>&1
  setsid nohup dotnet run --no-build --project src/QuanLyKho.Web --launch-profile http --urls http://0.0.0.0:5141 \
    > /tmp/quanlykho.log 2>&1 < /dev/null &
fi

# Mỗi lần bật lại, Codespaces đưa cổng về Private (chỉ chủ codespace mở được); đặt lại Public sau khi ứng dụng lên.
setsid nohup bash -c 'sleep 30; gh codespace ports visibility 5141:public -c "$CODESPACE_NAME"'   > /tmp/quanlykho-cong.log 2>&1 < /dev/null &
