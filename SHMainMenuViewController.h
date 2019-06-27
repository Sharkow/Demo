//
//  SHMainNavigationController.h
//  LRWest
//
//  Created by Михаил Акулов on 01.05.14.
//  Copyright (c) 2014 Sharkow. All rights reserved.
//

#import <UIKit/UIKit.h>
#import "SHDataHandler.h"
#import "SHMainMenuLoader.h"

@interface SHMainMenuViewController : UIViewController <UICollectionViewDataSource, UICollectionViewDelegate>

@property (copy, nonatomic)          NSNumber* showedUserCarId;
@property (weak, nonatomic) IBOutlet UIButton *offersButton;
@property (weak, nonatomic) IBOutlet UIImageView *activeCarImageView;
@property (weak, nonatomic) IBOutlet UICollectionView *loadedMenu;
@property (strong, nonatomic) IBOutlet UIScrollView *mainScrollView;
@property (weak, nonatomic) IBOutlet UIView *bottomButtonsContainer;

@property (strong, nonatomic) NSArray<MenuEntry*>* loadedMenuEntries;

-(IBAction)openShop:(UIButton *)sender;
-(void)handleSuccessOnSendingProductOrder:(UserOrder*) order;
-(void)handleErrorOnLoadingShop;

@end
