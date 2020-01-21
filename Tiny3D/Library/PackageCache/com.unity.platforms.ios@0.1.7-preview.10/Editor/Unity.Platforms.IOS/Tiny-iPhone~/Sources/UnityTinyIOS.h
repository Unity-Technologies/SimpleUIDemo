// methods exported by Unity,Tiny.IOS package

extern "C" {
void init(void *nwh, int width, int height);
void step(void);
void pauseapp(int paused);
void destroyapp(void);
void start(void);
void touchevent(int id, int action, int xpos, int ypos);
}
