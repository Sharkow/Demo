//
//  SHDataHandler.h
//  LRWest
//
//  Created by Михаил Акулов on 27.05.14.
//  Copyright (c) 2014 Sharkow. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "Car.h"
#import "Product.h"
#import "Reference.h"
#import "Service.h"
#import "Offer.h"

@interface ProductsCategory : NSObject

@property (nonatomic, copy, nullable)   NSNumber* categoryId;
@property (nonatomic, copy, nullable)   NSString* name;
@property (nonatomic, copy, nullable)   NSString* categoryDescription;
@property (nonatomic, copy, nullable)   NSString* imagePath;
@property (nonatomic, strong, nullable) ProductsCategory* parentCategory;
@property (nonatomic, strong, nullable) NSArray<ProductsCategory*>* subcategories;
@property (nonatomic, strong, nullable) NSArray<Product*>* products;

@end

@interface ProductInCart : NSObject

@property (nonatomic, strong, nullable) Product* product;
@property (nonatomic, copy, nullable)   NSNumber* quantity;

- (void) encodeWithCoder:(nonnull NSCoder *)encoder;
- (nonnull id) initWithCoder:(nonnull NSCoder *)decoder;

@end

@interface UserOrder : NSObject

@property (nonatomic, strong, nullable) NSNumber* orderId;
@property (nonatomic, copy,   nullable) NSDate* date;
@property (nonatomic, strong, nullable) NSArray<ProductInCart*>* products;

- (void) encodeWithCoder:(nonnull NSCoder *)encoder;
- (nonnull id) initWithCoder:(nonnull NSCoder *)decoder;

@end

@interface SHDataHandler : NSObject

+ (nonnull NSNumber*)   userCarID;
+ (nonnull Car*)        activeUserCar;
+ (void)                setUserCarByID: (nonnull NSNumber*) key;
+ (nonnull NSArray*)    allCarsOrdered;
+ (void)                initData;
+ (nonnull NSArray*)    activeUserCarReferences;
+ (nonnull NSArray*)    activeUserCarService;
/**Array of Mileage objects.*/
+ (nonnull NSArray*)    activeUserCarMaintenanceMileagesOrdered;
+ (nonnull NSArray*)    activeUserCarServiceForMileageOrdered: (nonnull Mileage*) mileage;

// If categories returned is nil, then the shop is unavailable.
+ (void) getUserCarProductsWithHandler:(nonnull void (^)(NSArray<ProductsCategory*>* _Nullable categories)) completionHandler;

/**Stores user contact data and desired service scheduling info.
 Stored in memory. To save it, call saveUserPreferences.
 User active Car is stored separately and can be different from one stored here!*/
+ (nonnull NSMutableDictionary*) userPreferences;
+ (void)                         saveUserPreferences;

+ (nonnull NSArray<ProductInCart*>*) userProductsCart;
+ (NSUInteger) getProductQuantityInCart:(nonnull Product*) product;
+ (void) setCartQuantity:(NSUInteger) qtty forProduct:(nonnull Product*) product;
+ (void) setCartQuantity:(NSUInteger) qtty at:(NSUInteger) index;
+ (void) removeProductFromCart:(nonnull Product*) product;
+ (void) removeProductFromCartAt:(NSUInteger) index;
+ (void) clearCart;

+ (nonnull NSArray<UserOrder*>*) userOrderHistory;
+ (void) addOrderIntoHistory:(nonnull UserOrder*) order;

/**An array of Offer objects, sorted by title.*/
+ (nonnull NSMutableArray*) activeUserCarOffersOrdered;

+ (nonnull NSNumber*) activeUserCarNewOffersCount;

@end

FOUNDATION_EXPORT NSString* const _Nullable NO_PRODUCT_PHOTO_IMAGE;

FOUNDATION_EXPORT NSString* const _Nullable USER_CAR_KEY;
FOUNDATION_EXPORT NSString* const _Nullable USER_NAME_KEY;
FOUNDATION_EXPORT NSString* const _Nullable USER_PHONE_KEY;
FOUNDATION_EXPORT NSString* const _Nullable USER_EMAIL_KEY;
FOUNDATION_EXPORT NSString* const _Nullable SERVICE_DATE_KEY;
FOUNDATION_EXPORT NSString* const _Nullable USER_SERVICE_COMMENTS_KEY;
FOUNDATION_EXPORT NSString* const _Nullable SERVICE_COMMENT_IS_AUTO_KEY;
FOUNDATION_EXPORT NSString* const _Nullable LAST_SERVICE_DATA_SUBMITTED_SUCCESSFULLY_KEY;
