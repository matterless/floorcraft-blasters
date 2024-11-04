#import "HapticManager.h"

void HapticFeedback(int type)
{
	[[HapticManager instance] hapticFeedback:type];
}
