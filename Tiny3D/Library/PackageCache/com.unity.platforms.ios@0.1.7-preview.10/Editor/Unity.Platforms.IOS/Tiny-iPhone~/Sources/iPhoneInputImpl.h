#pragma once

#import <UIKit/UIKit.h>

void InputInit(UIView* view);
void InputShutdown();
void InputProcess();
void ProcessTouchEvents(UIView* view, NSSet* touches, NSSet* allTouches);
