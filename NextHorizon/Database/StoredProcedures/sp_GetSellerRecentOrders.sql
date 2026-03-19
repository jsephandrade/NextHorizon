CREATE OR ALTER PROCEDURE dbo.sp_GetSellerRecentOrders
    @SellerId INT,
    @Top INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Top)
        o.OrderID               AS OrderId,
        o.FullName              AS Customer,
        o.ProductName,
        ISNULL(p.ImagePath, '') AS ProductImage,
        ISNULL(oi.Size, '')     AS Size,
        ''                      AS Sku,
        o.OrderDate             AS DateTime,
        ISNULL(o.DeliveryOption, '') AS Courier,
        o.Status,
        o.TotalAmount
    FROM dbo.Orders o
    LEFT JOIN dbo.OrderItems oi ON oi.OrderID = o.OrderID
    LEFT JOIN dbo.Products   p  ON p.ProductId = oi.ProductID
    WHERE o.seller_id = @SellerId
    ORDER BY o.OrderDate DESC, o.OrderID DESC;
END;
GO
