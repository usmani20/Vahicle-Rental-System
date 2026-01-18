using Microsoft.EntityFrameworkCore;
using Vahicle_Rental_System.Data;
using Vahicle_Rental_System.Models;

public static class DbSeeder
{
    public static void Seed(IApplicationBuilder applicationBuilder)
    {
        using (var serviceScope = applicationBuilder.ApplicationServices.CreateScope())
        {
            var context = serviceScope.ServiceProvider.GetService<AppDbContext>();

            // Ensure Database is Created
            context.Database.EnsureCreated();

            // Check if cars already exist
            if (!context.Cars.Any())
            {
                // Ensure Categories Exist
                if (!context.Categories.Any())
                {
                    context.Categories.AddRange(
                        new Category { Name = "Standard" },
                        new Category { Name = "SUV" },
                        new Category { Name = "Luxury" },
                        new Category { Name = "Sports" }
                    );
                    context.SaveChanges();
                }

                // Get Category IDs
                var standard = context.Categories.First(c => c.Name == "Standard").Id;
                var suv = context.Categories.First(c => c.Name == "SUV").Id;
                var luxury = context.Categories.First(c => c.Name == "Luxury").Id;
                var sports = context.Categories.First(c => c.Name == "Sports").Id;

                var cars = new List<Car>
                {
                    // --- STANDARD CARS ---
                    new Car { Brand="Toyota", Model="Corolla Altis", Description="Reliable and fuel-efficient sedan.", PriceSelfDrive=5000, PriceWithDriver=8000, OvertimeRate=500, FuelPolicy="Full to Full", Mileage="14 km/l", Luggage="2 Bags", FuelType="Petrol", Transmission="Automatic", Seats=5, CategoryId=standard, ImageUrl="/images/car-1.jpg", IsAvailable=true },
                    new Car { Brand="Honda", Model="Civic X", Description="Sporty look with comfortable interior.", PriceSelfDrive=6000, PriceWithDriver=9000, OvertimeRate=600, FuelPolicy="Same to Same", Mileage="12 km/l", Luggage="2 Bags", FuelType="Petrol", Transmission="Automatic", Seats=5, CategoryId=standard, ImageUrl="/images/car-2.jpg", IsAvailable=true },
                    new Car { Brand="Suzuki", Model="Cultus", Description="Compact hatchback perfect for city drives.", PriceSelfDrive=3500, PriceWithDriver=6000, OvertimeRate=300, FuelPolicy="Full to Full", Mileage="16 km/l", Luggage="1 Bag", FuelType="Petrol", Transmission="Manual", Seats=4, CategoryId=standard, ImageUrl="/images/car-3.jpg", IsAvailable=true },
                    new Car { Brand="Suzuki", Model="Swift", Description="Stylish hatchback with good performance.", PriceSelfDrive=4000, PriceWithDriver=6500, OvertimeRate=400, FuelPolicy="Same to Same", Mileage="13 km/l", Luggage="2 Bags", FuelType="Petrol", Transmission="Automatic", Seats=4, CategoryId=standard, ImageUrl="/images/car-4.jpg", IsAvailable=true },
                    new Car { Brand="Toyota", Model="Yaris", Description="Smooth drive and economical.", PriceSelfDrive=4500, PriceWithDriver=7000, OvertimeRate=450, FuelPolicy="Full to Full", Mileage="15 km/l", Luggage="2 Bags", FuelType="Petrol", Transmission="Automatic", Seats=5, CategoryId=standard, ImageUrl="/images/car-5.jpg", IsAvailable=true },
                    new Car { Brand="Honda", Model="City", Description="Comfortable sedan with spacious trunk.", PriceSelfDrive=5500, PriceWithDriver=8500, OvertimeRate=550, FuelPolicy="Same to Same", Mileage="14 km/l", Luggage="3 Bags", FuelType="Petrol", Transmission="Automatic", Seats=5, CategoryId=standard, ImageUrl="/images/car-6.jpg", IsAvailable=true },
                    new Car { Brand="Changan", Model="Alsvin", Description="Modern features at an affordable price.", PriceSelfDrive=4800, PriceWithDriver=7500, OvertimeRate=500, FuelPolicy="Full to Full", Mileage="13 km/l", Luggage="2 Bags", FuelType="Petrol", Transmission="Automatic", Seats=5, CategoryId=standard, ImageUrl="/images/car-7.jpg", IsAvailable=true },
                    new Car { Brand="Proton", Model="Saga", Description="Solid build quality and smooth ride.", PriceSelfDrive=4200, PriceWithDriver=6800, OvertimeRate=400, FuelPolicy="Same to Same", Mileage="12 km/l", Luggage="2 Bags", FuelType="Petrol", Transmission="Automatic", Seats=5, CategoryId=standard, ImageUrl="/images/car-8.jpg", IsAvailable=true },

                    // --- SUV CARS ---
                    new Car { Brand="Toyota", Model="Fortuner", Description="Powerful SUV for off-road and luxury.", PriceSelfDrive=15000, PriceWithDriver=20000, OvertimeRate=1500, FuelPolicy="Full to Full", Mileage="9 km/l", Luggage="4 Bags", FuelType="Diesel", Transmission="Automatic", Seats=7, CategoryId=suv, ImageUrl="/images/car-9.jpg", IsAvailable=true },
                    new Car { Brand="Kia", Model="Sportage", Description="Modern SUV with premium features.", PriceSelfDrive=12000, PriceWithDriver=16000, OvertimeRate=1200, FuelPolicy="Same to Same", Mileage="10 km/l", Luggage="3 Bags", FuelType="Petrol", Transmission="Automatic", Seats=5, CategoryId=suv, ImageUrl="/images/car-10.jpg", IsAvailable=true },
                    new Car { Brand="Hyundai", Model="Tucson", Description="Stylish SUV with panoramic sunroof.", PriceSelfDrive=12500, PriceWithDriver=16500, OvertimeRate=1250, FuelPolicy="Full to Full", Mileage="10 km/l", Luggage="3 Bags", FuelType="Petrol", Transmission="Automatic", Seats=5, CategoryId=suv, ImageUrl="/images/car-11.jpg", IsAvailable=true },
                    new Car { Brand="MG", Model="HS", Description="Luxury interior and advanced safety.", PriceSelfDrive=13000, PriceWithDriver=17000, OvertimeRate=1300, FuelPolicy="Same to Same", Mileage="9 km/l", Luggage="3 Bags", FuelType="Petrol", Transmission="Automatic", Seats=5, CategoryId=suv, ImageUrl="/images/car-12.jpg", IsAvailable=true },
                    new Car { Brand="Haval", Model="H6", Description="Hybrid technology with great power.", PriceSelfDrive=14000, PriceWithDriver=18000, OvertimeRate=1400, FuelPolicy="Full to Full", Mileage="18 km/l", Luggage="4 Bags", FuelType="Hybrid", Transmission="Automatic", Seats=5, CategoryId=suv, ImageUrl="/images/car-13.jpg", IsAvailable=true },
                    new Car { Brand="Toyota", Model="Prado", Description="The ultimate land cruiser experience.", PriceSelfDrive=25000, PriceWithDriver=30000, OvertimeRate=2000, FuelPolicy="Same to Same", Mileage="7 km/l", Luggage="5 Bags", FuelType="Diesel", Transmission="Automatic", Seats=7, CategoryId=suv, ImageUrl="/images/car-14.jpg", IsAvailable=true },
                    new Car { Brand="Glory", Model="580 Pro", Description="Spacious 7-seater for families.", PriceSelfDrive=11000, PriceWithDriver=15000, OvertimeRate=1100, FuelPolicy="Full to Full", Mileage="10 km/l", Luggage="4 Bags", FuelType="Petrol", Transmission="Automatic", Seats=7, CategoryId=suv, ImageUrl="/images/car-15.jpg", IsAvailable=true },

                    // --- LUXURY CARS ---
                    new Car { Brand="Mercedes", Model="C-Class", Description="Elegance and prestige combined.", PriceSelfDrive=20000, PriceWithDriver=25000, OvertimeRate=2500, FuelPolicy="Full to Full", Mileage="11 km/l", Luggage="2 Bags", FuelType="Petrol", Transmission="Automatic", Seats=5, CategoryId=luxury, ImageUrl="/images/car-16.jpg", IsAvailable=true },
                    new Car { Brand="Audi", Model="A6", Description="Advanced technology and comfort.", PriceSelfDrive=22000, PriceWithDriver=28000, OvertimeRate=2800, FuelPolicy="Same to Same", Mileage="12 km/l", Luggage="3 Bags", FuelType="Petrol", Transmission="Automatic", Seats=5, CategoryId=luxury, ImageUrl="/images/car-17.jpg", IsAvailable=true },
                    new Car { Brand="BMW", Model="5 Series", Description="Ultimate driving machine.", PriceSelfDrive=23000, PriceWithDriver=29000, OvertimeRate=3000, FuelPolicy="Full to Full", Mileage="10 km/l", Luggage="3 Bags", FuelType="Petrol", Transmission="Automatic", Seats=5, CategoryId=luxury, ImageUrl="/images/car-18.jpg", IsAvailable=true },
                    new Car { Brand="Toyota", Model="Crown", Description="Japanese luxury at its finest.", PriceSelfDrive=18000, PriceWithDriver=24000, OvertimeRate=2000, FuelPolicy="Same to Same", Mileage="14 km/l", Luggage="3 Bags", FuelType="Hybrid", Transmission="Automatic", Seats=5, CategoryId=luxury, ImageUrl="/images/car-19.jpg", IsAvailable=true },
                    new Car { Brand="Hyundai", Model="Sonata", Description="Futuristic design and luxury feel.", PriceSelfDrive=14000, PriceWithDriver=19000, OvertimeRate=1500, FuelPolicy="Full to Full", Mileage="12 km/l", Luggage="3 Bags", FuelType="Petrol", Transmission="Automatic", Seats=5, CategoryId=luxury, ImageUrl="/images/car-20.jpg", IsAvailable=true },
                    new Car { Brand="Lexus", Model="LX570", Description="Top-tier luxury SUV.", PriceSelfDrive=50000, PriceWithDriver=60000, OvertimeRate=5000, FuelPolicy="Same to Same", Mileage="6 km/l", Luggage="6 Bags", FuelType="Petrol", Transmission="Automatic", Seats=7, CategoryId=luxury, ImageUrl="/images/car-21.jpg", IsAvailable=true },
                    new Car { Brand="Range Rover", Model="Autobiography", Description="Royal comfort and status.", PriceSelfDrive=60000, PriceWithDriver=75000, OvertimeRate=6000, FuelPolicy="Full to Full", Mileage="7 km/l", Luggage="4 Bags", FuelType="Petrol", Transmission="Automatic", Seats=5, CategoryId=luxury, ImageUrl="/images/car-22.jpg", IsAvailable=true },

                    // --- SPORTS CARS ---
                    new Car { Brand="Porsche", Model="911", Description="Iconic sports car performance.", PriceSelfDrive=80000, PriceWithDriver=100000, OvertimeRate=8000, FuelPolicy="Full to Full", Mileage="8 km/l", Luggage="1 Bag", FuelType="Petrol", Transmission="Automatic", Seats=2, CategoryId=sports, ImageUrl="/images/car-23.jpg", IsAvailable=true },
                    new Car { Brand="Chevrolet", Model="Corvette", Description="American muscle power.", PriceSelfDrive=75000, PriceWithDriver=90000, OvertimeRate=7500, FuelPolicy="Same to Same", Mileage="7 km/l", Luggage="1 Bag", FuelType="Petrol", Transmission="Automatic", Seats=2, CategoryId=sports, ImageUrl="/images/car-24.jpg", IsAvailable=true },
                    new Car { Brand="Nissan", Model="GTR", Description="The Godzilla of roads.", PriceSelfDrive=70000, PriceWithDriver=85000, OvertimeRate=7000, FuelPolicy="Full to Full", Mileage="6 km/l", Luggage="2 Bags", FuelType="Petrol", Transmission="Automatic", Seats=4, CategoryId=sports, ImageUrl="/images/car-25.jpg", IsAvailable=true },
                    new Car { Brand="Ford", Model="Mustang", Description="Raw power and style.", PriceSelfDrive=55000, PriceWithDriver=70000, OvertimeRate=5500, FuelPolicy="Same to Same", Mileage="8 km/l", Luggage="2 Bags", FuelType="Petrol", Transmission="Automatic", Seats=4, CategoryId=sports, ImageUrl="/images/car-26.jpg", IsAvailable=true },
                    new Car { Brand="Audi", Model="R8", Description="Supercar performance.", PriceSelfDrive=90000, PriceWithDriver=110000, OvertimeRate=9000, FuelPolicy="Full to Full", Mileage="5 km/l", Luggage="1 Bag", FuelType="Petrol", Transmission="Automatic", Seats=2, CategoryId=sports, ImageUrl="/images/car-27.jpg", IsAvailable=true },
                    new Car { Brand="Lamborghini", Model="Huracan", Description="Extreme speed and looks.", PriceSelfDrive=120000, PriceWithDriver=150000, OvertimeRate=10000, FuelPolicy="Same to Same", Mileage="4 km/l", Luggage="0 Bags", FuelType="Petrol", Transmission="Automatic", Seats=2, CategoryId=sports, ImageUrl="/images/car-28.jpg", IsAvailable=true },
                    new Car { Brand="Ferrari", Model="488 Spider", Description="Open-top Italian mastery.", PriceSelfDrive=130000, PriceWithDriver=160000, OvertimeRate=12000, FuelPolicy="Full to Full", Mileage="4 km/l", Luggage="0 Bags", FuelType="Petrol", Transmission="Automatic", Seats=2, CategoryId=sports, ImageUrl="/images/car-29.jpg", IsAvailable=true },
                    new Car { Brand="Dodge", Model="Challenger", Description="Classic muscle car vibes.", PriceSelfDrive=50000, PriceWithDriver=65000, OvertimeRate=5000, FuelPolicy="Same to Same", Mileage="7 km/l", Luggage="3 Bags", FuelType="Petrol", Transmission="Automatic", Seats=5, CategoryId=sports, ImageUrl="/images/car-30.jpg", IsAvailable=true }
                };

                context.Cars.AddRange(cars);
                context.SaveChanges();
            }
        }
    }
}