DECLARE @i INT = 1
WHILE @i <= 50
BEGIN
    INSERT INTO Categories(Name, Slug, ParentId, IsActive)
    VALUES (
        N'Danh mục ' + CAST(@i AS NVARCHAR),
        'danh-muc-' + CAST(@i AS NVARCHAR),
        NULL,
        1
    )
    SET @i += 1
END

INSERT INTO MasterColors(Name, HexCode) VALUES
(N'Đỏ','#FF0000'),
(N'Xanh lá','#00FF00'),
(N'Xanh dương','#0000FF'),
(N'Đen','#000000'),
(N'Trắng','#FFFFFF'),
(N'Vàng','#FFFF00'),
(N'Hồng','#FFC0CB'),
(N'Tím','#800080'),
(N'Cam','#FFA500'),
(N'Xám','#808080')

INSERT INTO MasterSizes(Name) VALUES
('XS'),('S'),('M'),('L'),('XL'),('XXL'),('38'),('39')

SET @i = 1
WHILE @i <= 20
BEGIN
    INSERT INTO Promotions(
        Name, DiscountType, DiscountValue,
        StartDate, EndDate, IsActive, Priority
    )
    VALUES (
        N'Flash Sale ' + CAST(@i AS NVARCHAR),
        CASE WHEN RAND()>0.5 THEN 'Percentage' ELSE 'FixedAmount' END,
        FLOOR(RAND()*30)+5,
        DATEADD(DAY,-FLOOR(RAND()*10),GETDATE()),
        DATEADD(DAY,FLOOR(RAND()*30)+10,GETDATE()),
        1,
        FLOOR(RAND()*5)
    )
    SET @i += 1
END

SET @i = 1
WHILE @i <= 20
BEGIN
    INSERT INTO ArticleCategories(Name, Slug)
    VALUES (
        N'Danh mục tin ' + CAST(@i AS NVARCHAR),
        'tin-' + CAST(@i AS NVARCHAR)
    )
    SET @i += 1
END

SET @i = 1
WHILE @i <= 50
BEGIN
    INSERT INTO Products(
        Slug, CategoryId, Name, Description,
        Price, Thumbnail, IsActive
    )
    VALUES (
        'product-' + CAST(@i AS NVARCHAR),
        FLOOR(RAND()*50)+1,
        N'Sản phẩm ' + CAST(@i AS NVARCHAR),
        N'Mô tả chi tiết sản phẩm ' + CAST(@i AS NVARCHAR),
        FLOOR(RAND()*900000)+100000,
        'thumb.jpg',
        1
    )
    SET @i += 1
END

SET @i = 1
WHILE @i <= 100
BEGIN
    INSERT INTO ProductVariants(
        ProductId, ColorId, SizeId,
        Sku, Quantity, PriceModifier
    )
    VALUES (
        FLOOR(RAND()*50)+1,
        FLOOR(RAND()*10)+1,
        FLOOR(RAND()*8)+1,
        'SKU-' + CAST(@i AS NVARCHAR),
        FLOOR(RAND()*200)+10,
        FLOOR(RAND()*50000)
    )
    SET @i += 1
END

SET @i = 1
WHILE @i <= 100
BEGIN
    INSERT INTO ProductVariants(
        ProductId, ColorId, SizeId,
        Sku, Quantity, PriceModifier
    )
    VALUES (
        FLOOR(RAND()*50)+1,
        FLOOR(RAND()*10)+1,
        FLOOR(RAND()*8)+1,
        'SKU-' + CAST(@i AS NVARCHAR),
        FLOOR(RAND()*200)+10,
        FLOOR(RAND()*50000)
    )
    SET @i += 1
END

SET @i = 1
WHILE @i <= 20
BEGIN
    INSERT INTO PromotionConditions(PromotionId, Field, Operator, Value)
    VALUES (
        FLOOR(RAND()*20)+1,
        'TotalAmount',
        '>=',
        '500000'
    )
    SET @i += 1
END

SET @i = 1
WHILE @i <= 50
BEGIN
    INSERT INTO Articles(
        CategoryId, Title, Slug,
        Summary, Content, Thumbnail
    )
    VALUES (
        FLOOR(RAND()*20)+1,
        N'Bài viết ' + CAST(@i AS NVARCHAR),
        'bai-viet-' + CAST(@i AS NVARCHAR),
        N'Tóm tắt bài viết',
        N'Nội dung chi tiết bài viết',
        'thumb.jpg'
    )
    SET @i += 1
END





SET @i = 1
WHILE @i <= 50
BEGIN
    DECLARE @Total DECIMAL(18,2) = FLOOR(RAND()*2000000)+200000
    DECLARE @Discount DECIMAL(18,2) = FLOOR(@Total*0.1)
    DECLARE @Ship DECIMAL(18,2) = 30000

    INSERT INTO Orders(
        OrderCode, OrderDate, UserId,
        ShippingName, ShippingAddress, ShippingPhone,
        TotalAmount, DiscountAmount, ShippingFee, FinalAmount,
        CouponCode, PaymentMethod, PaymentStatus, Status
    )
    VALUES (
        'ORD' + RIGHT('000'+CAST(@i AS NVARCHAR),4),
        DATEADD(DAY,-FLOOR(RAND()*30),GETDATE()),
        FLOOR(RAND()*5)+1,
        N'Khách hàng ' + CAST(@i AS NVARCHAR),
        N'TP.HCM',
        '09' + CAST(FLOOR(RAND()*90000000)+10000000 AS NVARCHAR),
        @Total,
        @Discount,
        @Ship,
        @Total-@Discount+@Ship,
        NULL,
        CASE WHEN RAND()>0.5 THEN 'COD' ELSE 'Banking' END,
        'Paid',
        FLOOR(RAND()*4)
    )

    SET @i += 1
END

SET @i = 1
WHILE @i <= 100
BEGIN
    INSERT INTO OrderDetails(
        OrderId, SnapshotProductName,
        SnapshotSKU, Quantity, UnitPrice
    )
    VALUES (
        FLOOR(RAND()*50)+1,
        N'Sản phẩm snapshot',
        'SKU-' + CAST(FLOOR(RAND()*100)+1 AS NVARCHAR),
        FLOOR(RAND()*5)+1,
        FLOOR(RAND()*900000)+100000
    )
    SET @i += 1
END

SET @i = 1
WHILE @i <= 50
BEGIN
    INSERT INTO OrderStatusHistory(
        OrderId, OldStatus, NewStatus,
        UpdateByType, UpdateByUserId
    )
    VALUES (
        FLOOR(RAND()*50)+1,
        0,
        FLOOR(RAND()*4),
        1,
        FLOOR(RAND()*5)+1
    )
    SET @i += 1
END

SET @i = 1
WHILE @i <= 50
BEGIN
    INSERT INTO ProductReviews(
        ProductId, UserId, OrderId,
        Rating, Comment
    )
    VALUES (
        FLOOR(RAND()*50)+1,
        FLOOR(RAND()*5)+1,
        FLOOR(RAND()*50)+1,
        FLOOR(RAND()*5)+1,
        N'Đánh giá mức ' + CAST(FLOOR(RAND()*5)+1 AS NVARCHAR)
    )
    SET @i += 1
END

SET @i = 1
WHILE @i <= 50
BEGIN
    INSERT INTO CartItems(
        UserId, ProductId, ProductVariantId, Quantity
    )
    VALUES (
        FLOOR(RAND()*5)+1,
        FLOOR(RAND()*50)+1,
        FLOOR(RAND()*100)+1,
        FLOOR(RAND()*5)+1
    )
    SET @i += 1
END

SET @i = 1
WHILE @i <= 50
BEGIN
    INSERT INTO Coupons(
        Code, UserId, PromotionId,
        IsUsed, ExpiryDate
    )
    VALUES (
        'CODE' + CAST(@i AS NVARCHAR),
        FLOOR(RAND()*5)+1,
        FLOOR(RAND()*20)+1,
        0,
        DATEADD(DAY,FLOOR(RAND()*60),GETDATE())
    )
    SET @i += 1
END

SET @i = 1
WHILE @i <= 50
BEGIN
    INSERT INTO Notifications(
        Title, Content, Type, UserId
    )
    VALUES (
        N'Thông báo ' + CAST(@i AS NVARCHAR),
        N'Nội dung thông báo hệ thống',
        N'Đơn hàng',
        FLOOR(RAND()*5)+1
    )
    SET @i += 1
END