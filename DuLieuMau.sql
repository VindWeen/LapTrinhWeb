go
use LapTrinhWeb
go
INSERT INTO Users (Username, Password, Email, Role, IsLocked)
VALUES
(N'admin', N'hash_admin', N'admin@gmail.com', 'Admin', 0),
(N'staff01', N'hash_staff', N'staff@gmail.com', 'Staff', 0),
(N'nguyenvana', N'hash_customer1', N'vana@gmail.com', 'Customer', 0),
(N'tranthib', N'hash_customer2', N'thib@gmail.com', 'Customer', 0);
go
INSERT INTO UseAddresses (UserId, ContactName, ContactPhone, AddressLine, Province, District, Ward, IsDefault)
VALUES
(3, N'Nguyễn Văn A', '0909000001', N'123 Lê Lợi', N'Đồng Nai', N'Biên Hòa', N'Tân Phong', 1),
(4, N'Trần Thị B', '0909000002', N'456 Nguyễn Huệ', N'TP.HCM', N'Quận 1', N'Bến Nghé', 1);
go
INSERT INTO Categories (Name, Slug, ParentId, IsActive)
VALUES
(N'Áo', 'ao', NULL, 1),
(N'Áo Thun', 'ao-thun', 1, 1),
(N'Quần', 'quan', NULL, 1);
go
INSERT INTO Products (Slug, CategoryId, Name, Description, Price, Thumbnail, IsActive)
VALUES
('ao-thun-basic', 2, N'Áo Thun Basic', N'Áo thun cotton 100%', 199000, 'ao1.jpg', 1),
('quan-jean-slimfit', 3, N'Quần Jean Slimfit', N'Jean co giãn nhẹ', 499000, 'quan1.jpg', 1);
go
INSERT INTO ProductImage (ProductId, ImageUrl, SortOrder)
VALUES
(1, 'ao1_1.jpg', 1),
(1, 'ao1_2.jpg', 2),
(2, 'quan1_1.jpg', 1);
go
INSERT INTO MasterColors (Name, HexCode)
VALUES
(N'Đen', '#000000'),
(N'Trắng', '#FFFFFF'),
(N'Xanh', '#0000FF');
go
INSERT INTO MasterSizes (Name)
VALUES
('S'),
('M'),
('L');
go
INSERT INTO ProductVariants (ProductId, ColorId, SizeId, Sku, Quantity, PriceModifier)
VALUES
(1, 1, 1, 'ATB-ĐEN-S', 50, 0),
(1, 2, 2, 'ATB-TRANG-M', 30, 0),
(2, 1, 2, 'QJ-ĐEN-M', 20, 0);
go
INSERT INTO Promotions (Name, DiscountType, DiscountValue, StartDate, EndDate, IsActive, Priority)
VALUES
(N'Giảm 10%', 'Percentage', 10, '2026-01-01', '2026-12-31', 1, 1),
(N'Giảm 50K', 'FixedAmount', 50000, '2026-01-01', '2026-12-31', 1, 2);
go
INSERT INTO PromotionConditions (PromotionId, Field, Operator, Value)
VALUES
(1, 'TotalAmount', '>=', '300000'),
(2, 'CategoryId', '=', '2');
go
INSERT INTO ProductPromotions (ProductId, PromotionId)
VALUES
(1, 1),
(2, 2);
go
INSERT INTO Coupons (Code, UserId, PromotionId, IsUsed, ExpiryDate)
VALUES
('GIAM10', 3, 1, 0, '2026-12-31'),
('SALE50K', 4, 2, 0, '2026-12-31');
go
INSERT INTO CartItems (UserId, ProductId, ProductVariantId, Quantity)
VALUES
(3, 1, 1, 2),
(4, 2, 3, 1);
go
INSERT INTO Orders (OderCode, UserId, ShippingName, ShippingAddrress, ShippingPhone,
TotalAmount, DiscountAmount, ShippingFee, FinalAmount,
PaymentMethod, PaymentStatus, Status)
VALUES
('ORD001', 3, N'Nguyễn Văn A', N'123 Lê Lợi', '0909000001',
398000, 39800, 30000, 388200,
'COD', 'Pending', 0);
go
INSERT INTO OderDetails (OrderId, SnapshotProductName, SnapshotSKU, Quantity, UnitPrice)
VALUES
(1, N'Áo Thun Basic', 'ATB-ĐEN-S', 2, 199000);
go
INSERT INTO OderStatusHistory (OrderId, OldStatus, NewStatus, UpdateByType, Note)
VALUES
(1, NULL, 0, 0, N'Tạo đơn hàng');
go
INSERT INTO ArticleCategories (Name, Slug)
VALUES
(N'Tin tức', 'tin-tuc'),
(N'Khuyến mãi', 'khuyen-mai');
go
INSERT INTO Articles (CategoryId, Title, Slug, Summary, Content, Thumbnail)
VALUES
(1, N'Ra mắt bộ sưu tập mới', 'ra-mat-bo-suu-tap',
N'Giới thiệu BST 2026',
N'Nội dung chi tiết bài viết...',
'news1.jpg');
go
INSERT INTO ProductReviews (ProductId, UserId, OrderId, Rating, Comment)
VALUES
(1, 3, 1, 5, N'Sản phẩm rất tốt, sẽ ủng hộ tiếp!');
go
INSERT INTO Notifications (Title, Content, Type, UserId)
VALUES
(N'Đơn hàng mới', N'Bạn có đơn hàng ORD001', N'Đơn hàng', 3),
(N'Khuyến mãi tháng 3', N'Giảm giá 10% toàn bộ sản phẩm', N'Khuyến mãi', NULL);
