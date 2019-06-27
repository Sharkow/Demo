//
//  SHMainNavigationController.m
//  LRWest
//
//  Created by Михаил Акулов on 01.05.14.
//  Copyright (c) 2014 Sharkow. All rights reserved.
//

#import "SHMainMenuViewController.h"
#import "SHCarsTableViewController.h"
#import "SHProductsViewController.h"
#import "SHDataHandler.h"
#import "MBProgressHUD.h"
#import "CustomBadge.h"
#import <SDWebImage/UIButton+WebCache.h>
#import "SHLoadedMenuViewController.h"
#import "SHWebReferenceViewController.h"

@implementation SHMainMenuViewController
{
    CustomBadge* newOffersBadge;
    BOOL _activityIndicatorShown;
}

//static NSString* const NOT_FIRST_LAUNCH_SETTING =   @"notFirstLaunchKey";

- (id)initWithNibName:(NSString *)nibNameOrNil bundle:(NSBundle *)nibBundleOrNil
{
    self = [super initWithNibName:nibNameOrNil bundle:nibBundleOrNil];
    if (self) {
        // Custom initialization
    }
    return self;
}

- (void)didReceiveMemoryWarning
{
    [super didReceiveMemoryWarning];
    // Dispose of any resources that can be recreated.
}

-(void)refreshOffersBadge
{
    [newOffersBadge removeFromSuperview];
    int newOffers = [[SHDataHandler activeUserCarNewOffersCount] intValue];
    if (newOffers > 0)
    {
        if (newOffersBadge == nil)
        {
            BadgeStyle* badgeStyle = [BadgeStyle freeStyleWithTextColor:[UIColor blackColor]
                                                         withInsetColor:[UIColor whiteColor]
                                                         withFrameColor:[UIColor lightGrayColor]
                                                              withFrame: YES
                                                             withShadow: NO
                                                            withShining: NO
                                                           withFontType: BadgeStyleFontTypeHelveticaNeueLight];
            newOffersBadge = [CustomBadge customBadgeWithString:[NSString stringWithFormat:@"%d", newOffers]
                                                      withStyle:badgeStyle];
            
            CGRect rect = CGRectMake(self.offersButton.frame.size.width - newOffersBadge.frame.size.width/2,
                                     -newOffersBadge.frame.size.height/2,
                                     newOffersBadge.frame.size.width, newOffersBadge.frame.size.height);
            [newOffersBadge setFrame:rect];
            [self.offersButton addSubview:newOffersBadge];
        }
        else
        {
            newOffersBadge.badgeText = [NSString stringWithFormat:@"%d", newOffers];
            [newOffersBadge setNeedsDisplay];
        }
        [self.offersButton addSubview:newOffersBadge];
    }
}

-(void) updateUserCar
{
    NSLog(@"SHLOG updating car...");
    NSUInteger currentMenuEntriesCount = self.loadedMenuEntries == nil ? 1 : self.loadedMenuEntries.count;
    self.loadedMenuEntries = nil;
    [self updateLoadedMenuWithOldItemCount:currentMenuEntriesCount];
    self.showedUserCarId = [SHDataHandler activeUserCar].id;
    self.activeCarImageView.image = [UIImage imageNamed: [SHDataHandler activeUserCar].icon];
    [self refreshOffersBadge];
    [SHMainMenuLoader getUserCarMenuWithHandler:^(NSArray<MenuEntry *> * _Nullable menuButtons) {
        self.loadedMenuEntries = menuButtons;
        [self updateLoadedMenuWithOldItemCount:1];
        NSLog(@"SHLOG car updated, new item count: %lu", [self collectionView:self.loadedMenu numberOfItemsInSection:0]);
    }];
}

-(void)viewDidAppear:(BOOL)animated
{
    [super viewDidAppear:animated];
    
    if ([SHDataHandler activeUserCar] == nil)
    {
        [self performSegueWithIdentifier:@"FirstLaunchSegue" sender:nil];
    }
    else
    {
        if((self.showedUserCarId == nil)
        || [self.showedUserCarId compare: [SHDataHandler activeUserCar].id] != NSOrderedSame)
        {
            [self updateUserCar];
        }
    }
}


- (IBAction)callLRWest:(UIButton *)sender
{
    NSString *phoneNumber = [@"tel://" stringByAppendingString:
                            [[NSBundle mainBundle] objectForInfoDictionaryKey: @"LRWest phone number"]];
    [[UIApplication sharedApplication] openURL:[NSURL URLWithString:phoneNumber]
                                       options:@{}
                              completionHandler:^(BOOL success) { } ];
}

-(void)handleSuccessOnSendingProductOrder:(nonnull UserOrder*) order
{
    [self.navigationController popToRootViewControllerAnimated:YES];
    
    MBProgressHUD* successHUD = [MBProgressHUD showHUDAddedTo:self.view animated:YES];
    successHUD.mode = MBProgressHUDModeText;
    successHUD.label.font = [UIFont systemFontOfSize:19];
    successHUD.label.textColor = [UIColor colorWithRed:10/255.F green:170/255.F blue:0 alpha:1];
    successHUD.label.text = @"Мы получили ваш заказ.";
    successHUD.detailsLabel.font = [UIFont systemFontOfSize:14];
    successHUD.detailsLabel.text = @"Скоро мы вам перезвоним, чтобы уточнить данные.";
    successHUD.removeFromSuperViewOnHide = YES;
    [successHUD hideAnimated:YES afterDelay:4];
    
    [SHDataHandler addOrderIntoHistory:order];
}

-(void)handleErrorOnLoadingShop
{
    [self.navigationController popToRootViewControllerAnimated:true];
    
    MBProgressHUD* progressHUD = [MBProgressHUD showHUDAddedTo:self.view animated:true];
    progressHUD.mode = MBProgressHUDModeText;
    progressHUD.label.font = [UIFont systemFontOfSize:19];
    progressHUD.label.textColor = [UIColor colorWithRed:10/255.F green:170/255.F blue:0 alpha:1];
    progressHUD.label.text = @"Нет доступа к магазину";
    progressHUD.detailsLabel.font = [UIFont systemFontOfSize:14];
    progressHUD.detailsLabel.text = @"Ошибка подключения";
    progressHUD.removeFromSuperViewOnHide = true;
    [progressHUD hideAnimated:YES afterDelay:4];
}

#pragma mark - Navigation

// In a storyboard-based application, you will often want to do a little preparation before navigation
- (void)prepareForSegue:(UIStoryboardSegue *)segue sender:(id)sender
{
    // Get the new view controller using [segue destinationViewController].
    // Pass the selected object to the new view controller.
    if ([segue.identifier isEqual: @"showLoadedSubmenu"])
    {
        MenuEntry* selectedMenuEntry = sender;
        SHLoadedMenuViewController* dest = (SHLoadedMenuViewController*)segue.destinationViewController;
        dest.showedMenuEntries = selectedMenuEntry.subentries;
        dest.navigationItem.title = selectedMenuEntry.title;
    }
    else if ([segue.identifier isEqual: @"showWebUrl"])
    {
        MenuEntry* selectedMenuEntry = sender;
        SHWebReferenceViewController* dest = (SHWebReferenceViewController*)segue.destinationViewController;
        dest.showedUrl = selectedMenuEntry.url;
        dest.navigationItem.title = selectedMenuEntry.title;
    }
}

-(void)setActivityIndicatorShown:(BOOL)activityIndicatorShown
          manageNetworkIndicator:(BOOL)manageNetworkIndicator
                       manageHUD:(BOOL)manageHUD
{
    if (activityIndicatorShown == _activityIndicatorShown) return;
    
    _activityIndicatorShown = activityIndicatorShown;
    if (activityIndicatorShown)
    {
        self.navigationItem.rightBarButtonItem.enabled = NO;
        self.navigationItem.leftBarButtonItem.enabled = NO;
        if (manageNetworkIndicator) [UIApplication sharedApplication].networkActivityIndicatorVisible = YES;
        if (manageHUD) [MBProgressHUD showHUDAddedTo:self.view animated:YES];
    }
    else
    {
        self.navigationItem.rightBarButtonItem.enabled = YES;
        self.navigationItem.leftBarButtonItem.enabled = YES;
        if (manageNetworkIndicator) [UIApplication sharedApplication].networkActivityIndicatorVisible = NO;
        if (manageHUD) [MBProgressHUD hideHUDForView:self.view animated:YES];
    }
}

- (IBAction)openShop:(UIButton *)sender
{
    [self setActivityIndicatorShown:true manageNetworkIndicator:true manageHUD:true];
    [SHDataHandler getUserCarProductsWithHandler:^(NSArray<ProductsCategory*>* _Nullable categories)
     {
         [self setActivityIndicatorShown:false manageNetworkIndicator:true manageHUD:true];
         if (categories == nil)
         {
             [self handleErrorOnLoadingShop];
         }
         else
         {
             UIStoryboard *mainStoryboard = [UIStoryboard storyboardWithName:@"Storyboard" bundle: nil];
             SHProductsViewController* shopViewController =
             (SHProductsViewController*)[mainStoryboard instantiateViewControllerWithIdentifier:@"SHProductsViewController"];
             shopViewController.categories = categories;
             shopViewController.products = [[NSArray alloc] init];
             [self.navigationController pushViewController:shopViewController animated:YES];
         }
     }];
}

///////////////////// Пункты меню, загружаемые с сервера ////////////////////////////

-(NSInteger)collectionView:(UICollectionView *)collectionView numberOfItemsInSection:(NSInteger)section
{
    NSInteger result = self.loadedMenuEntries == nil ? 1 : self.loadedMenuEntries.count;
    NSLog(@"SHLOG number of items loaded: %li", (long)result);
    return result;
}

-(UICollectionViewCell *)collectionView:(UICollectionView *)collectionView cellForItemAtIndexPath:(NSIndexPath *)indexPath
{
    UICollectionViewCell* cell;
    if (self.loadedMenuEntries == nil) {
        cell = [collectionView dequeueReusableCellWithReuseIdentifier:@"LoadIndicator" forIndexPath:indexPath];
    }
    else {
        cell = [collectionView dequeueReusableCellWithReuseIdentifier:@"Cell" forIndexPath:indexPath];
        UIButton* button = (UIButton*)[cell.contentView viewWithTag:1];
        MenuEntry* entry = [self.loadedMenuEntries objectAtIndex:indexPath.row];
        [button setTitle: entry.title forState:UIControlStateNormal];
        [button sd_setImageWithURL: [NSURL URLWithString: entry.imagePath] forState: UIControlStateNormal];
    }
    return cell;
}

- (CGSize)collectionView:(UICollectionView *)collectionView
                  layout:(UICollectionViewLayout *)collectionViewLayout
  sizeForItemAtIndexPath:(NSIndexPath *)indexPath
{
    if (self.loadedMenuEntries == nil) return CGSizeMake(self.loadedMenu.frame.size.width, 60);
    else return CGSizeMake(135, 135);
}

-(void)updateLoadedMenuWithOldItemCount:(NSUInteger) oldCount
{
    NSLog(@"SHLOG updating menu...");
    [self.loadedMenu performBatchUpdates:^{
        NSMutableArray<NSIndexPath*>* cellsToDelete = [NSMutableArray arrayWithCapacity:oldCount];
        for (NSUInteger i = 0; i < oldCount; ++i) {
            [cellsToDelete addObject:[NSIndexPath indexPathForRow:i inSection:0]];
        }
        [self.loadedMenu deleteItemsAtIndexPaths:cellsToDelete];
        
        NSUInteger newCellsCount = self.loadedMenuEntries == nil ? 1 : self.loadedMenuEntries.count;
        NSMutableArray<NSIndexPath*>* cellsToAdd =
        [NSMutableArray arrayWithCapacity:newCellsCount];
        for (NSUInteger i = 0; i < newCellsCount; ++i) {
            [cellsToAdd addObject:[NSIndexPath indexPathForRow:i inSection:0]];
        }
        [self.loadedMenu insertItemsAtIndexPaths:cellsToAdd];
    } completion:^(BOOL finished)
     {
         NSLog(@"SHLOG animation comleted, count: %ld", (long)[self collectionView:self.loadedMenu numberOfItemsInSection:0]);
         [self.loadedMenu invalidateIntrinsicContentSize];
         [self.mainScrollView setNeedsLayout];
         [self.mainScrollView layoutIfNeeded];
         CGPoint bottomButtonsOrigin = [self.mainScrollView convertPoint:CGPointZero fromView:self.bottomButtonsContainer];
         self.mainScrollView.contentSize = CGSizeMake(self.view.frame.size.width,
                                                      bottomButtonsOrigin.y
                                                      + self.bottomButtonsContainer.frame.size.height);
     }];
}
- (IBAction)loadedMenuButtonAction:(UIButton *)sender
{
    NSInteger selectedIndex = [self.loadedMenu indexPathForCell:(UICollectionViewCell*)sender.superview.superview].row;
    MenuEntry* selectedMenuEntry = _loadedMenuEntries[selectedIndex];
    if (selectedMenuEntry.subentries.count > 0) {
        [self performSegueWithIdentifier:@"showLoadedSubmenu" sender:selectedMenuEntry];
    }
    else {
        [self performSegueWithIdentifier:@"showWebUrl" sender:selectedMenuEntry];
    }
}

///////////////////// END OF: Пункты меню, загружаемые с сервера ////////////////////////////


@end
