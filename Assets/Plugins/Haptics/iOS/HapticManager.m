#import "Foundation/Foundation.h"
#import "HapticManager.h"
#import "UIKit/UIKit.h"
#import "GameKit/GameKit.h"

@implementation HapticManager

+ (HapticManager *)instance
{
    static HapticManager *instance = nil;
    if( !instance )
        instance = [[HapticManager alloc] init];
    return instance;
}

- (void)hapticFeedback : (int) type
{
	UIImpactFeedbackGenerator *hap = [[UIImpactFeedbackGenerator alloc] init];
    
    [hap prepare];
    
    switch (type) {
        case 0:
            [hap initWithStyle:UIImpactFeedbackStyleLight];
            break;
        case 1:
            [hap initWithStyle:UIImpactFeedbackStyleMedium];
            break;
        case 2:
            [hap initWithStyle:UIImpactFeedbackStyleHeavy];
            break;
        case 3:
            [hap initWithStyle:UIImpactFeedbackStyleRigid];
            break;
        case 4:
            [hap initWithStyle:UIImpactFeedbackStyleSoft];
            break;
        default:
            [hap initWithStyle:UIImpactFeedbackStyleLight];
            break;
    }
    
    [hap impactOccurred];
}

@end
