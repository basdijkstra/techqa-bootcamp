using NUnit.Framework;
using PixelVault.IntegrationTests.Models;
using static RestAssured.Dsl;

namespace PixelVault.IntegrationTests.Exercises;

[TestFixture]
public class Exercises04 : CatalogApiTestBase
{
    [Test]
    public void ReserveAndReleaseStock_AvailableQuantityGoesDownAndBackUp()
    {
        /*
         * TODO: Retrieve the details for the game with ID 1 by sending a GET request to {CatalogBaseUrl}/games/1
         * and extract the value of the availableQuantity field in the response using .Extract().Body("$.availableQuantity").
         * Store it in a variable.
         *
         * Note: .Extract().Body() returns an object. Use Convert.ToInt32() to turn it into an int. 
         */


        /*
         * TODO: Create a ReservationRequest object (see Models/Reservation.cs) with:
         * - OrderReference: "ANSWERS04-SCENARIO"
         * - Items: one ReservationRequestItem for game 1, quantity 2
         * 
         * Send it as the request body in a POST request to {CatalogBaseUrl}/reservations
         * and verify that the response status code is 201 (Created)
         * and extract the value of the Location response header using .Extract().Header("Location").
         * This header contains the URL of the reservation that was just created.
         */ 


        /*
         * TODO: Send a GET request to the URL from the Location header and verify that the reservation status is "Active".
         */


        /*
         * TODO: Retrieve game 1 again and verify that availableQuantity is now 2 lower than before we created the reservation.
         */


        /*
         * TODO: Release the reservation, again using the extracted URL
         * by sending a POST request to {reservationUrl}/release and verify that the reservation status is now "Released".
         */


        /*
         * TODO: Verify that the released copies are available again by retrieving game 1 again and verifying that availableQuantity is back to its original value.
         */

    }
}
