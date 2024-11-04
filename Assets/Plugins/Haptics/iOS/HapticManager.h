#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <GameKit/GameKit.h>

@interface HapticManager : NSObject { }

+ (HapticManager*) instance;

- (void) hapticFeedback : (int) type;

@end
