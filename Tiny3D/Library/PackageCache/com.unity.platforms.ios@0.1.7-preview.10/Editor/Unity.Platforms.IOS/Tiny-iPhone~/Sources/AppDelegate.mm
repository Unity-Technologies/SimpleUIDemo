#import "AppDelegate.h"
#import "UnityTinyIOS.h"

@implementation AppDelegate

@synthesize m_window;
@synthesize m_view;

- (BOOL)application:(UIApplication *)application didFinishLaunchingWithOptions:(NSDictionary *)launchOptions {
    // setting path to read resources
    NSString *placeholderFile = @"placeholder";
    NSString *path = [[NSBundle mainBundle] pathForResource:placeholderFile ofType:@""];
    chdir([path substringToIndex: (path.length - placeholderFile.length)].UTF8String);
    
    start();
    
    CGRect rect = [ [UIScreen mainScreen] bounds];
    m_window = [ [UIWindow alloc] initWithFrame: rect];
    m_view = [ [TinyView alloc] initWithFrame: rect];
    [m_window addSubview: m_view];
    
    UIViewController *viewController = [[TinyViewController alloc] init];
    viewController.view = m_view;
    
    [m_window setRootViewController:viewController];
    [m_window makeKeyAndVisible];
    
    float scaleFactor = [[UIScreen mainScreen] scale];
    [m_view setContentScaleFactor: scaleFactor];
    
    return YES;
}


- (void)applicationWillResignActive:(UIApplication *)application {
    [m_view stop];
    pauseapp(1);
}


- (void)applicationDidEnterBackground:(UIApplication *)application {
}


- (void)applicationWillEnterForeground:(UIApplication *)application {
}


- (void)applicationDidBecomeActive:(UIApplication *)application {
    pauseapp(0);
    [m_view start];
}


- (void)applicationWillTerminate:(UIApplication *)application {
    destroyapp();
}

@end
