#import <UIKit/UIKit.h>

@interface TinyView : UIView
{
    CADisplayLink* m_displayLink;
}

- (void)start;
- (void)stop;

@end

@interface TinyViewController : UIViewController

@end
