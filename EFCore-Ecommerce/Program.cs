using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

Console.WriteLine();


var context = new EcommerceContext();

// await InsertData();
// await ReadData();
// await ExpressionFunc();

Console.WriteLine("Done!");

#region DataManipulation

async Task ExpressionFunc()
{
    Func<User, bool> f = u => u.Username.StartsWith("A");
    Expression<Func<User, bool>> ex = u => u.Username.StartsWith("A");

    var users = await context.Users
        .Where(ex)
        .ToArrayAsync();

    var users2 = new User[]
    {
        new User() { Username = "Alexandre" }
    };

    users2.Where(u => u.Username.StartsWith("A"));
}

async Task InsertData()
{
    User user1, user2;
    Product product1, product2, product3;

    // Inserting Users
    await context.Users.AddRangeAsync(new User[]
    {
        user1 = new User() { Username = "Giorgi", Email = "giorgi@email.com", PasswordHash = "hash1" },
        user2 = new User() { Username = "Luka", Email = "luka@email.com", PasswordHash = "hash2" }
    });

    await context.SaveChangesAsync();

    // Inserting Products
    await context.Products.AddRangeAsync(new Product[]
    {
        product1 = new Product() { Name = "Soccer ball", Description = "A round ball for playing soccer", Price = 25m, StockQuantity = 100 },
        product2 = new Product() { Name = "Cowboy hat", Description = "A stylish cowboy hat", Price = 10m, StockQuantity = 50 },
        product3 = new Product() { Name = "Blue pen", Description = "A blue ink pen", Price = 1.5m, StockQuantity = 200 }
    });

    await context.SaveChangesAsync();

    // Inserting Order
    Order order1;

    await context.Orders.AddAsync(
        order1 = new Order()
        {
            OrderDate = DateTime.Now,
            User = user1,
            OrderItems = new[]
            {
                new OrderItem() { Product = product1, Quantity = 1, Price = product1.Price },
                new OrderItem() { Product = product2, Quantity = 2, Price = product2.Price }
            }
        });

    await context.SaveChangesAsync();

    // Insert Payment
    await context.Payments.AddAsync(new Payment()
    {
        Order = order1,
        PaymentDate = DateTime.Now,
        Amount = order1.OrderItems.Sum(oi => oi.TotalPrice),
        PaymentMethod = "Credit Card",
        PaymentStatus = "Completed"
    });

    await context.SaveChangesAsync();
}

async Task ReadData()
{
    // Reading Users with their Orders
    var users = await context.Users
        .Include(u => u.Orders)
        .ThenInclude(o => o.OrderItems)
        .ThenInclude(oi => oi.Product)  // Including Products in OrderItems
        .ToArrayAsync();

    // Reading Orders with their related Users and Products
    var orders = await context.Orders
        .Include(o => o.User)
        .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.Product)
        .ToArrayAsync();

    foreach (var order in orders)
    {
        Console.WriteLine($"Order {order.Id} - ordered by {order.User.Username} with items: {string.Join(", ", order.OrderItems.Select(oi => $"{oi.Product.Name} (x{oi.Quantity})"))}");
    }

    // Read specific User and their associated Orders
    var user1 = await context.Users
        .Include(u => u.Orders)
        .ThenInclude(o => o.OrderItems)
        .ThenInclude(oi => oi.Product)
        .FirstOrDefaultAsync(u => u.Id == 1);

    Console.WriteLine($"User: {user1?.Username}, Orders: {user1?.Orders.Count}");

    // Read a specific order and associated Payment
    var orderWithPayment = await context.Orders
        .Include(o => o.Payment)
        .FirstOrDefaultAsync(o => o.Id == 1);

    if (orderWithPayment?.Payment != null)
    {
        Console.WriteLine($"Order {orderWithPayment.Id} has payment of {orderWithPayment.Payment.Amount} via {orderWithPayment.Payment.PaymentMethod}");
    }
}

#endregion

#region Entities

// Description:
//1.User(or Customer) – This class represents the user who shops on the platform.
//2. Product – A detailed version of the Item class, representing products for sale.
//3. Category – A category to group products (e.g., Electronics, Clothing, Home Appliances).
//4. Cart – A shopping cart that holds items a user intends to purchase.
//5. CartItem – This class represents an item in the user's shopping cart.
//6. Order – A finalized order from the user.
//7. OrderItem – Represents the many-to-many relationship between orders and products.
//8. Payment – Payment details for an order.
//9. Address – Shipping address for the user (different from the shipping address in the order).
//10. Wishlist – A wishlist for products that a user may want to purchase later.
//11. WishlistItem – Items that the user has added to their wishlist.
//12. Review – A review left by the customer for a product they purchased.


class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public DateTime RegisteredAt { get; set; }
    public ICollection<Order> Orders { get; set; }
    public ICollection<CartItem> CartItems { get; set; }  // Represents items in the cart
}

class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; }  // Many-to-many with Order
    public ICollection<CartItem> CartItems { get; set; }  // Represents products in the cart
}

class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public ICollection<Product> Products { get; set; }
}

class Cart
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public ICollection<CartItem> CartItems { get; set; }
    public decimal TotalAmount { get; set; }  // Calculated from CartItems
    public DateTime CreatedAt { get; set; }
}

class CartItem
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public Cart Cart { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }  // Price * Quantity
}

class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }  // Calculated from OrderItems
    public ICollection<OrderItem> OrderItems { get; set; }
    public Payment Payment { get; set; }
    public string ShippingAddress { get; set; } = null!;
    public string Status { get; set; } = "Pending";  // E.g., "Shipped", "Delivered"
}

class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TotalPrice => Price * Quantity;  // Price * Quantity
}

class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = null!;  // E.g., Credit Card, PayPal
    public string PaymentStatus { get; set; } = "Pending";  // E.g., "Completed", "Failed"
}

class Address
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public string AddressLine1 { get; set; } = null!;
    public string AddressLine2 { get; set; } = null!;
    public string City { get; set; } = null!;
    public string State { get; set; } = null!;
    public string PostalCode { get; set; } = null!;
    public string Country { get; set; } = null!;
}

class Wishlist
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public ICollection<WishlistItem> WishlistItems { get; set; }
}

class WishlistItem
{
    public int Id { get; set; }
    public int WishlistId { get; set; }
    public Wishlist Wishlist { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }
}

class Review
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public int Rating { get; set; }  // Rating from 1 to 5
    public string Comment { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}



#endregion



#region DbContext

class EcommerceContext : DbContext
{
    // DbSets for entities
    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<Wishlist> Wishlists { get; set; }
    public DbSet<WishlistItem> WishlistItems { get; set; }
    public DbSet<Review> Reviews { get; set; }

    // Configuring the DbContext to use SQL Server
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=EcommerceDb;Trusted_Connection=True;")
                      .LogTo(Console.WriteLine, LogLevel.Information);
    }
}

#endregion