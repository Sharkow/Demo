//
//  SHDataHandler.m
//  LRWest
//
//  Created by Михаил Акулов on 27.05.14.
//  Copyright (c) 2014 Sharkow. All rights reserved.
//

#import "SHDataHandler.h"
#import "AFNetworking.h"

@implementation ProductsCategory
@end

@implementation ProductInCart

- (void) encodeWithCoder:(nonnull NSCoder *)encoder
{
    //Encode properties, other class variables, etc
    [encoder encodeObject:self.product forKey:@"product"];
    [encoder encodeObject:self.quantity forKey:@"quantity"];
}

- (nonnull id) initWithCoder:(nonnull NSCoder *)decoder
{
    if((self = [super init])) {
        //decode properties, other class vars
        self.product = [decoder decodeObjectForKey:@"product"];
        self.quantity = [decoder decodeObjectForKey:@"quantity"];
    }
    return self;
}

@end

@implementation UserOrder

- (void) encodeWithCoder:(nonnull NSCoder *)encoder
{
    //Encode properties, other class variables, etc
    [encoder encodeObject:self.orderId forKey:@"orderId"];
    [encoder encodeObject:self.date forKey:@"date"];
    [encoder encodeObject:self.products forKey:@"products"];
}

- (nonnull id) initWithCoder:(nonnull NSCoder *)decoder
{
    if((self = [super init])) {
        //decode properties, other class vars
        self.orderId = [decoder decodeObjectForKey:@"orderId"];
        self.date = [decoder decodeObjectForKey:@"date"];
        self.products = [decoder decodeObjectForKey:@"products"];
    }
    return self;
}

@end

@implementation SHDataHandler

#pragma mark Constants

NSString* const _Nullable NO_PRODUCT_PHOTO_IMAGE = @"BlankProductImage";

//static NSString* const USER_CARS_KEY_SETTING       =   @"userCarsKeys";
//static NSString* const ACTIVE_USER_CAR_KEY_SETTING  =   @"activeUserCarKey";
static NSString* const USER_CAR_KEY_SETTING             =   @"userCarKey";
static NSString* const DB_FILE_NAME                     =   @"gl2.sqlite";
static NSString* const USER_SERVICE_SCHEDULE_SETTING    =   @"userServiceScheduleInfo";
static NSString* const USER_ORDERS_SETTING              =   @"userOrders";

NSString* const USER_CAR_KEY                            = @"userCar";
NSString* const USER_NAME_KEY                           = @"userName";
NSString* const USER_PHONE_KEY                          = @"userPhone";
NSString* const USER_EMAIL_KEY                          = @"userEmail";
NSString* const SERVICE_DATE_KEY                        = @"serviceDate";
NSString* const USER_SERVICE_COMMENTS_KEY               = @"serviceComments";

//Is 1 if the last comment for service schedule was set automatically, when tapping "Schedule" button in Maintenance table or in an Offer details view.
NSString* const SERVICE_COMMENT_IS_AUTO_KEY              = @"serviceCommentIsMileage";

//You need to clear this setting every time when changing user preferences for any fields in Schedule Service View controller.
NSString* const LAST_SERVICE_DATA_SUBMITTED_SUCCESSFULLY_KEY= @"lastServiceDataSubmittedSuccessfuly";

#pragma mark Variables

static Car* _activeUserCar;
static NSMutableDictionary* _userServiceScheduleInfo;
static NSMutableArray<ProductInCart*>* _userProductsCart;
static NSMutableArray<UserOrder*>* _userOrders;

#pragma mark Public Methods

+(NSArray*) allCarsOrdered
{
    return [Car instancesOrderedBy:@"name"];
}

+(NSNumber*) userCarID
{
    return [[NSUserDefaults standardUserDefaults] objectForKey:USER_CAR_KEY_SETTING];
}

+ (Car*) activeUserCar
{
    return _activeUserCar;
}

+ (void) setUserCarByID:(NSNumber*) key
{
    //If user chose a different car, his products cart should be cleared.
    if([self userCarID] != nil && ![key isEqualToNumber:[self userCarID]]
    && _userProductsCart != nil)
    {
        [_userProductsCart removeAllObjects];
    }
    
    [[NSUserDefaults standardUserDefaults] setObject: key forKey: USER_CAR_KEY_SETTING];
    _activeUserCar = [Car instanceWithPrimaryKey:key createIfNonexistent:NO];
    
    //When user changes his Car, let us change his car setting in Service Schedule
    [_userServiceScheduleInfo setObject:_activeUserCar forKey:USER_CAR_KEY];
}

+ (void) initData
{
    if ([FCModel databaseIsOpen]) return;
    [FCModel openDatabaseAtPath: [self dbPath]
             withDatabaseInitializer:NULL
             schemaBuilder:^(FMDatabase *db, int *schemaVersion) {}];
    
    _activeUserCar = [Car instanceWithPrimaryKey: [self userCarID] createIfNonexistent:NO];
    
    if ([self userPreferences] == nil)
        _userServiceScheduleInfo = [[NSMutableDictionary alloc] init];
}

+ (NSArray*) activeUserCarReferences
{
    return [Reference instancesWhere:
        @"id IN (SELECT record_id FROM ReferenceToCar r WHERE r.car_id = ?) "
        "ORDER BY name",
        _activeUserCar.id];
}

+ (NSArray*) activeUserCarService
{
    return [Service instancesWhere:
        @"id IN (SELECT service_id FROM ServiceToCar p WHERE p.car_id = ?) "
        "ORDER BY name",
        _activeUserCar.id];
}

+ (NSArray*) activeUserCarMaintenanceMileagesOrdered
{
    NSArray* concatenatedMileages = [FCModel firstColumnArrayFromQuery:
            @"SELECT DISTINCT (mileage || 'delimiter' || mileage_2012) FROM Service WHERE id IN "
                "(SELECT service_id FROM ServiceToCar p WHERE p.car_id = ?) "
            "ORDER BY mileage",
            _activeUserCar.id];
    
    NSMutableArray* mileages = [NSMutableArray arrayWithCapacity:concatenatedMileages.count];
    for (int i = 0; i < concatenatedMileages.count; i++)
    {
        Mileage* mileage = [[Mileage alloc] init];;
        NSArray* splitMileagesString = [((NSString*)concatenatedMileages[i]) componentsSeparatedByString:@"delimiter"];
        mileage.mileageBefore2012 = ((NSString*)splitMileagesString[0]).intValue;
        mileage.mileageAfter2012 = ((NSString*)splitMileagesString[1]).intValue;
        [mileages addObject:mileage];
    }
    return [NSArray arrayWithArray:mileages];
}

+ (NSArray*) activeUserCarServiceForMileageOrdered:(Mileage*) mileage
{
    NSArray* serviceIdAndPrice = [FCModel resultDictionariesFromQuery:
                                  @"SELECT s.id service_id, stc.price "
                                  "FROM Service s JOIN ServiceToCar stc ON (s.id = stc.service_id) "
                                  "WHERE stc.car_id = ? AND s.mileage = ? AND s.mileage_2012 = ? "
                                  "ORDER BY s.service_order, s.name",
                                  _activeUserCar.id,
                                  [NSNumber numberWithInt: mileage.mileageBefore2012],
                                  [NSNumber numberWithInt: mileage.mileageAfter2012]];
    
    NSMutableArray* result = [NSMutableArray arrayWithCapacity:serviceIdAndPrice.count];
    for (int i = 0; i < serviceIdAndPrice.count; i++)
    {
        Service* service = [Service firstInstanceWhere:@"id = ?", serviceIdAndPrice[i][@"service_id"]];
        service.price = serviceIdAndPrice[i][@"price"];
        [result addObject:service];
    }
    return result;
}

+ (NSMutableDictionary*)userPreferences
{
    if (_userServiceScheduleInfo == nil)
    {
        _userServiceScheduleInfo =
        [[[NSUserDefaults standardUserDefaults] objectForKey:USER_SERVICE_SCHEDULE_SETTING] mutableCopy];
        
        //Cast Car ID to Car and store it in settings Dictionary
        [self restoreCarFromIDinUserServiceScheduleInfo];
    }
    
    return _userServiceScheduleInfo;
}

+ (void) saveUserPreferences
{
    //If schedule info was never queried, we are sure that it never changed - no need to save anything.
    if (_userServiceScheduleInfo == nil) return;
    
    //Can't save Car object to plist, so cast it to ID.
    NSObject* savedCar = [_userServiceScheduleInfo objectForKey:USER_CAR_KEY];
    if ([savedCar isKindOfClass:[Car class]])
        [_userServiceScheduleInfo setObject:[(Car*)savedCar id] forKey:USER_CAR_KEY];
    [[NSUserDefaults standardUserDefaults] setObject: _userServiceScheduleInfo forKey: USER_SERVICE_SCHEDULE_SETTING];
    [self restoreCarFromIDinUserServiceScheduleInfo];
}

+(void) restoreCarFromIDinUserServiceScheduleInfo
{
    NSNumber* savedCarId = [_userServiceScheduleInfo objectForKey:USER_CAR_KEY];
    if (savedCarId != nil)
        [_userServiceScheduleInfo setObject:[Car instanceWithPrimaryKey:savedCarId createIfNonexistent:NO]
                                     forKey:USER_CAR_KEY];
}

+ (nonnull Product*) parseProductFromDictionary:(nonnull NSDictionary*) dict
{
    Product* result = [[Product alloc] init];
    result.productId                = dict[@"id"];
    result.sku                      = [dict[@"SKU"] description];
    result.name                     = dict[@"name"];
    result.quantityInStock          = dict[@"quantity"];
    result.shortDescription         = dict[@"short_description"];
    result.longDescription          = dict[@"description"];
    result.price                    = dict[@"price"];
    result.imageId                  = dict[@"image_id"];
    result.productUrl               = dict[@"url"];
    return result;
}

/* products returned to handler is nil on error.
   If there are no products in category, and empty array is returned. */
+ (void) getProductsForSingleCategoryId:(nonnull NSNumber*) categoryId withHandler:(nonnull void (^)(NSArray<Product*>* _Nullable products)) completionHandler
{
    NSDictionary* parameters = @{@"cat_id" : categoryId};
    AFHTTPSessionManager *manager = [AFHTTPSessionManager manager];
    [manager GET:@"http://censored" parameters:parameters progress:nil success:^(NSURLSessionTask *task, id responseObject)
     {
         NSArray* productsToParse = (NSArray*)responseObject;
         NSMutableArray<Product*>* result = [[NSMutableArray alloc] initWithCapacity:productsToParse.count];
         for (NSDictionary* productToParse in productsToParse) {
             [result addObject:[self parseProductFromDictionary:productToParse]];
         }
         completionHandler(result);
     } failure:^(NSURLSessionTask *operation, NSError *error)
     {
         NSLog(@"SHLOG: Products network failure!");
         completionHandler(nil);
     }];
}

+ (void) getProductsForCategoryRecursive:(nonnull ProductsCategory*) category withCategoriesLoadCounter:(nonnull NSInteger*) leftToLoad withHandler:(nonnull void (^)(bool error)) completionHandler
{
    //NSLog(@"SHLOG: before adding: %i", (int)*leftToLoad);
    //NSLog(@"SHLOG: adding: %lu", category.subcategories.count);
    *leftToLoad += category.subcategories.count;
    //NSLog(@"SHLOG: added: %i", (int)*leftToLoad);
    
    for (ProductsCategory* subcategory in category.subcategories)
    {
        [self getProductsForCategoryRecursive:subcategory
                    withCategoriesLoadCounter:leftToLoad
                                  withHandler:completionHandler];
    }
    
    [self getProductsForSingleCategoryId:category.categoryId
                             withHandler:^(NSArray<Product *> * _Nullable products)
     {
         if (products == nil) {
             NSLog(@"Product load failure!");
             completionHandler(true);
         }
         else {
             category.products = products;
             //NSLog(@"SHLOG: decresing: %i", (int)*leftToLoad);
             --(*leftToLoad);
             if (*leftToLoad == 0) {
                 completionHandler(false);
             }
             //NSLog(@"SHLOG: decreased: %i", (int)*leftToLoad);
         }
     }];
}

static NSInteger categoriesLoadCounter;

+ (void) getAllProductsForCategories:(nonnull NSArray<ProductsCategory*>*) categories withHandler:(nonnull void (^)(bool error)) completionHandler
{
    __block bool handlerWasCalled = false;
    categoriesLoadCounter = categories.count;
    
    for (ProductsCategory* category in categories)
    {
        [self getProductsForCategoryRecursive:category
                    withCategoriesLoadCounter:&categoriesLoadCounter
                                  withHandler:^(bool error)
         {
             if (!handlerWasCalled) {
                 handlerWasCalled = true;
                 completionHandler(error);
             }
         }];
    }
}


+ (nonnull ProductsCategory*) parseCategoryFromDictionary:(nonnull NSDictionary*) dict withParent:(nullable ProductsCategory*) parent
{
    ProductsCategory* result = [[ProductsCategory alloc] init];
    result.parentCategory          = parent;
    result.categoryId              = dict[@"id"];
    result.name                    = dict[@"name"];
    result.categoryDescription     = dict[@"description"];
    result.imagePath               = dict[@"image"];
    
    if (dict[@"children"] != nil) {
        NSArray<NSDictionary*>* parsedSubcategories = dict[@"children"];
        NSMutableArray<ProductsCategory*>* subcategories =
            [[NSMutableArray alloc] initWithCapacity:parsedSubcategories.count];
        for (NSDictionary* parsedSubcategory in parsedSubcategories) {
            [subcategories addObject: [self parseCategoryFromDictionary:parsedSubcategory withParent:result]];
        }
        result.subcategories = subcategories;
    }
    else {
        result.subcategories = @[];
    }
    
    return result;
}

+ (void) getUserCarProductsWithHandler:(nonnull void (^)(NSArray<ProductsCategory*>* _Nullable categories)) completionHandler
{
    NSDictionary* parameters = @{@"car_id" : [self userCarID]};
    AFHTTPSessionManager *manager = [AFHTTPSessionManager manager];
    [manager GET:@"http://censored" parameters:parameters progress:nil success:^(NSURLSessionTask *task, id responseObject)
    {
        @try
        {
            NSArray* categoriesToParse = (NSArray*)responseObject;
            NSMutableArray<ProductsCategory*>* result =
                [[NSMutableArray alloc] initWithCapacity:categoriesToParse.count];
        
            for (NSDictionary* categoryToParse in categoriesToParse)
            {
                [result addObject:[self parseCategoryFromDictionary:categoryToParse withParent:nil]];
            }

            [self getAllProductsForCategories:result withHandler:^(bool error)
            {
                if (error)
                {
                    NSLog(@"SHLOG: Products loading error!");
                    completionHandler(nil);
                }
                else
                {
                    NSLog(@"SHLOG: Products loading done!");
                    [self removeObsoleteProductsFromCartWithNewShop:result];
                    completionHandler(result);
                }
            }];
        }
        @catch (NSException *exception) {
            NSLog(@"SHLOG: Parsing failure!");
            completionHandler(nil);
        }
        
    } failure:^(NSURLSessionTask *operation, NSError *error)
    {
        NSLog(@"SHLOG: Network failure!");
        completionHandler(nil);
    }];
}

+ (bool) category:(nonnull ProductsCategory*) category containsProduct:(nonnull NSNumber*) productId
{
    for (Product* product in category.products)
        if ([productId isEqualToNumber:product.productId])
            return true;
    
    for (ProductsCategory* subcategory in category.subcategories)
        if ([self category:subcategory containsProduct:productId])
            return true;
    
    return false;
}

+ (bool) shop:(nonnull NSArray<ProductsCategory*>*) categories containsProduct:(nonnull NSNumber*) productId
{
    for (ProductsCategory* category in categories)
        if ([self category:category containsProduct:productId])
            return true;
    
    return false;
}

+ (void) removeObsoleteProductsFromCartWithNewShop:(nonnull NSArray<ProductsCategory*>*) categories
{
    if (_userProductsCart == nil)
        return;
    
    for (NSInteger i = _userProductsCart.count - 1; i >= 0; --i)
        if (![self shop:categories containsProduct:_userProductsCart[i].product.productId])
            [_userProductsCart removeObjectAtIndex:i];
}

+ (nonnull NSArray<ProductInCart*>*) userProductsCart
{
    if (_userProductsCart == nil) _userProductsCart = [[NSMutableArray alloc] init];
    return _userProductsCart;
}


+ (NSUInteger) getProductQuantityInCart:(nonnull Product*) product
{
    for (ProductInCart* productInCart in [self userProductsCart])
    {
        if (productInCart.product.productId == product.productId) {
            return productInCart.quantity.unsignedIntValue;
        }
    }
    return 0;
}

+ (void) setCartQuantity:(NSUInteger) qtty forProduct:(nonnull Product*) product
{
    if (qtty == 0) {
        [self removeProductFromCart:product];
        return;
    }
    for (ProductInCart* productInCart in [self userProductsCart])
    {
        if (productInCart.product.productId == product.productId) {
            productInCart.quantity = [NSNumber numberWithUnsignedInteger: qtty];;
            return;
        }
    }
    ProductInCart* toAdd = [[ProductInCart alloc] init];
    toAdd.product = product;
    toAdd.quantity = [NSNumber numberWithUnsignedInteger: qtty];
    [_userProductsCart addObject:toAdd];
}

+ (void) setCartQuantity:(NSUInteger) qtty at:(NSUInteger) index
{
    _userProductsCart[index].quantity = [NSNumber numberWithInteger:qtty];
}

+ (void) removeProductFromCart:(nonnull Product*) product
{
    if (_userProductsCart == nil) return;
    
    for (NSInteger i = _userProductsCart.count - 1; i >= 0; --i)
    {
        if (product.productId == [_userProductsCart objectAtIndex:i].product.productId) {
            [_userProductsCart removeObjectAtIndex:i];
        }
    }
}

+ (void) clearCart
{
    if (_userProductsCart == nil) return;
    [_userProductsCart removeAllObjects];
}

+ (void) removeProductFromCartAt:(NSUInteger) index
{
    [_userProductsCart removeObjectAtIndex:index];
}

+ (nonnull NSArray<UserOrder*>*) userOrderHistory
{
    NSArray<NSData*>* history = [[NSUserDefaults standardUserDefaults] objectForKey:USER_ORDERS_SETTING];
    NSMutableArray<UserOrder*>* result = [NSMutableArray arrayWithCapacity:history.count];
    for (NSData* encodedOrder in history)
         [result addObject:[NSKeyedUnarchiver unarchiveObjectWithData:encodedOrder]];
    return result;
}

+ (void) addOrderIntoHistory:(nonnull UserOrder*) order
{
    NSData* encodedOrder = [NSKeyedArchiver archivedDataWithRootObject:order];
    NSArray<NSData*>* history = [[NSUserDefaults standardUserDefaults] objectForKey:USER_ORDERS_SETTING];
    NSArray<NSData*>* newHistory = (history == nil) ?
        @[encodedOrder] : [history arrayByAddingObject:encodedOrder];
    [[NSUserDefaults standardUserDefaults] setObject: newHistory forKey: USER_ORDERS_SETTING];
}

+ (nonnull NSMutableArray *)activeUserCarOffersOrdered
{
    NSMutableArray* offers = [NSMutableArray arrayWithArray:[self activeUserCarOffers]];
    [offers sortUsingComparator:^NSComparisonResult(id obj1, id obj2) {
        
        //Filter out any non-letters from titles, for comparison to ignore symbols such as " ( ) - : etc.
        NSString* filteredOfferTitle1 =
        [[((Offer*)obj1).title componentsSeparatedByCharactersInSet:
          [[NSCharacterSet letterCharacterSet] invertedSet]] componentsJoinedByString:@""];
        NSString* filteredOfferTitle2 =
        [[((Offer*)obj2).title componentsSeparatedByCharactersInSet:
          [[NSCharacterSet letterCharacterSet] invertedSet]] componentsJoinedByString:@""];
        
        return [filteredOfferTitle1 localizedCaseInsensitiveCompare: filteredOfferTitle2];
    }];
    return offers;
}

+ (nonnull NSNumber *)activeUserCarNewOffersCount
{
    NSArray* offers = [self activeUserCarOffers];
    int count = 0;
    for (Offer* offer in offers)
        if ([offer.is_new isEqualToNumber:[NSNumber numberWithInt:1]]) count++;
    return [NSNumber numberWithInt:count];
}

# pragma mark Private Methods

+(NSString*) dbPath
{
    return [[[NSBundle mainBundle] resourcePath] stringByAppendingPathComponent: DB_FILE_NAME];
}

+(NSArray*) activeUserCarOffers
{
    NSArray* offers =
    [Offer instancesWhere:@"id IN (SELECT offer_id FROM OffersToCar WHERE car_id IN(-1, ?))", _activeUserCar.id];
    return offers;
}

@end
