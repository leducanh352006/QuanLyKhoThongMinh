#!/usr/bin/env bash
# Chạy ứng dụng ở cổng 5141 (nếu chưa chạy) và mở cổng cho người ngoài xem.
# Gọi mỗi lần codespace bật lên hoặc mở lại cửa sổ, kể cả sau khi codespace tự tắt vì để lâu không dùng.
cd "$(dirname "$0")/.."

if ! pgrep -f '[Q]uanLyKho.Web' > /dev/null; then
  dotnet build src/QuanLyKho.Web -v q > /tmp/quanlykho-build.log 2>&1
  setsid nohup dotnet run --no-build --project src/QuanLyKho.Web --launch-profile http --urls http://0.0.0.0:5141 \
    > /tmp/quanlykho.log 2>&1 < /dev/null &
fi

# Mỗi lần bật lại, Codespaces đưa cổng về Private (chỉ chủ codespace mở được).
# Chờ ứng dụng trả lời (tối đa 5 phút) rồi đặt cổng Public, thử lại 3 lần; không được thì ghi rõ cách làm tay.
cat > /tmp/quanlykho-mo-cong.sh << 'EOF'
for i in $(seq 1 60); do
  curl -s -o /dev/null http://localhost:5141/ && break
  sleep 5
done
for lan in 1 2 3; do
  if gh codespace ports visibility 5141:public -c "$CODESPACE_NAME"; then
    echo "Đã đặt cổng 5141 Public."
    exit 0
  fi
  sleep 10
done
echo "KHÔNG đặt được cổng 5141 Public (xem lỗi ở trên). Làm tay: tab Cổng, chuột phải dòng 5141, Khả năng hiển thị cổng, Public."
exit 1
EOF
setsid nohup bash /tmp/quanlykho-mo-cong.sh > /tmp/quanlykho-cong.log 2>&1 < /dev/null &
echo "Ứng dụng đang khởi động ở cổng 5141. Kết quả mở cổng: cat /tmp/quanlykho-cong.log"
