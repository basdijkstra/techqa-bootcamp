export class MockResponses {

    static getBasketResponseWithPriceChangedForItem(basketId: string) {
        return {
              id: basketId,
              status: 'Active',
              items: [
                {
                  gameId: 1,
                  slug: 'ashes-of-valdren',
                  title: 'Ashes of Valdren',
                  category: 'Rpg',
                  ageRating: 'M',
                  minimumAge: 17,
                  isPreOrder: false,
                  releaseDate: '2024-03-14',
                  quantity: 2,
                  unitPrice: 54.99,
                  unitPriceWhenAdded: 59.99,
                  priceChanged: true,
                  lineTotal: 109.98,
                  stockStatus: 'InStock',
                  availableQuantity: 25,
                  issues: [],
                },
              ],
              itemCount: 2,
              subtotal: 109.98,
              bundleDiscount: 0,
              promoCode: null,
              appliedDiscount: 'None',
              discountTotal: 0,
              discountDescription: null,
              total: 109.98,
              minimumAge: 17,
              latestReleaseDate: null,
              hasIssues: false,
              canCheckout: true,
              messages: ['The price of one or more items has changed since you added them.'],
            }
    }

    static getBasketResponseWithStockIssue(basketId: string) {
        return {
            id: basketId,
            status: 'Active',
            items: [
                {
                gameId: 1,
                slug: 'ashes-of-valdren',
                title: 'Ashes of Valdren',
                category: 'Rpg',
                ageRating: 'M',
                minimumAge: 17,
                isPreOrder: false,
                releaseDate: '2024-03-14',
                quantity: 3,
                unitPrice: 59.99,
                unitPriceWhenAdded: 59.99,
                priceChanged: false,
                lineTotal: 179.97,
                stockStatus: 'LowStock',
                availableQuantity: 1,
                issues: ['Only 1 left in stock. Please lower the quantity.'],
                },
            ],
            itemCount: 3,
            subtotal: 179.97,
            bundleDiscount: 0,
            promoCode: null,
            appliedDiscount: 'None',
            discountTotal: 0,
            discountDescription: null,
            total: 179.97,
            minimumAge: 17,
            latestReleaseDate: null,
            hasIssues: true,
            canCheckout: false,
            messages: ['Some items in your basket need attention before you can check out.'],
            }
        }

    static getInternalServerErrorResponse() {
        return {
            type: 'https://tools.ietf.org/html/rfc9110#section-15.6.1',
            title: 'An error occurred while processing your request.',
            status: 500,
            code: 'ERROR',
        }
    }
}