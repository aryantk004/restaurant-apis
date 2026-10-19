using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Restaurants_Area_NotBlank",
                table: "Restaurants",
                sql: "LEN(LTRIM(RTRIM([Area]))) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Restaurants_City_NotBlank",
                table: "Restaurants",
                sql: "LEN(LTRIM(RTRIM([City]))) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Restaurants_DeliveryTime",
                table: "Restaurants",
                sql: "[DeliveryTimeMinutes] BETWEEN 1 AND 180");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Restaurants_Name_NotBlank",
                table: "Restaurants",
                sql: "LEN(LTRIM(RTRIM([Name]))) > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Restaurants_Area_NotBlank",
                table: "Restaurants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Restaurants_City_NotBlank",
                table: "Restaurants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Restaurants_DeliveryTime",
                table: "Restaurants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Restaurants_Name_NotBlank",
                table: "Restaurants");
        }
    }
}
