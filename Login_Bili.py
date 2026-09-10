# 有个缺点，我在青龙装不了playwright，所以我就本地弄了个直接写在文件里，然后自动打开文件显示cookie。
# 第三方包playwright, ddddocr, pillow
# 安装方式：
# playwright: 
#     pip install playwright   # 安装playwright，这个做爬虫比较简单
#     playwright install       # 安装playwright用到的浏览器内核
# ddddocr:
#     pip install ddddocr      # 这个开源识别包挺好用，虽然不是特别准，但是我觉得够用
# pillow
#     pip install pillow       # 这个是用来处理图片相关的
from playwright.sync_api import Playwright, sync_playwright, expect
import ddddocr
from PIL import Image

import time
import os
from io import BytesIO

################################################################################
USER_NAME = '用户名'  # 用户名
USER_PASSWD = '密码'  # 密码
FILE_ABS_PATH = os.path.abspath(__file__)
PATH = FILE_ABS_PATH.replace(os.path.split(__file__)[-1], '')  # 我用这个路径存放cookie
MAX_LOGIN_TRY_TIME = 8
# 登录按钮位置，这个可以自己使用playwright设置浏览器窗口大小，然后用画图软件查看对应位置
# 页面 https://passport.bilibili.com/login?gourl=https://space.bilibili.com
LOGIN_X, LOGIN_Y = 770, 490
# 验证码点完后点击【确认】位置
CONFIRM_X, CONFIRM_Y = 970, 550
# 截取验证码位置
HEAD_PIC = {"x":880,"y":240,"width":130,"height":40}  # 截取需要点击的文字图片
CONTENT_X_START, CONTENT_Y_START = 755, 280           # 截取内容图片左上角位置
CONTENT_PIC = {"x":CONTENT_X_START,"y":CONTENT_Y_START,"width":256,"height":256}  # 截取内容图片
################################################################################
class OCR:  # 根据截取的图片，识别要点击的内容，返回要点击的位置
    def __init__(self) -> None:
        self.ocr1 = ddddocr.DdddOcr(show_ad=False) # 识别器
        self.ocr2 = ddddocr.DdddOcr(det=True, show_ad=False) # 目标检测器
        self.head_ocr = None # 前面文字内容识别结果存储
        self.content_ocr = dict() # 位置识别结果存储位置

    def __call__(self):
        self.__ocr_head()  # 识别前面的文字
        self.__ocr_content()  # 识别有文字的位置
        return self.__judge()  # 返回要点击的位置

    def __ocr_head(self):  # 文字识别
        with open(os.path.join(PATH, 'head.png'), "rb")as fp:
            response = fp.read()
        self.head_ocr = self.ocr1.classification(response)
        print(f"HEAD_ddddocr: {self.head_ocr}")

    def __ocr_content(self):  # 内容识别
        with open(os.path.join(PATH, 'content.png'), "rb")as fp:
            response = fp.read()
        img = Image.open(BytesIO(response))

        detect_res = self.ocr2.detection(response)  # 识别文字位置
        for box in detect_res:
            x1, y1, x2, y2 = box
            tmp_img = img.crop((x1, y1, x2, y2))
            res = self.ocr1.classification(tmp_img)  # 识别文字
            if len(res):
                self.content_ocr[res] = ((x1+x2)//2, (y1+y2)//2)
            print(f"CONTENT_ddddocr: {res} {(x1+x2)//2} {(y1+y2)//2}")

    def __judge(self):
        '''
        能识别的且对应的文字确定后，不能识别的就随便给前面识别出来却没用到的位置
        我在自己电脑试了下，准确率还挺高的，出乎我的预料的好，一般三次内都能成功
        '''
        not_exist = []
        res = []
        res_content = []
        for c in range(len(self.head_ocr)):
            pos = self.content_ocr.get(self.head_ocr[c], None)
            res.append(pos)
            if not pos:
                res_content.append(pos)
                not_exist.append(c)
            else:
                res_content.append(self.head_ocr[c])
        not_exist_id = 0
        for d in self.content_ocr.keys():
            if d not in self.head_ocr:
                res[not_exist[not_exist_id]] = self.content_ocr[d]
                res_content[not_exist[not_exist_id]] = d
                not_exist_id += 1
                if not_exist_id == len(not_exist):
                    break
        return res, res_content

def run(playwright: Playwright) -> None:
    browser = playwright.chromium.launch(headless=True)  # False会打开浏览器，直接看到操作过程，True不打开浏览器看不到操作过程
    context = browser.new_context()
    page = context.new_page()
    page.goto("https://space.bilibili.com")  # 打开网页
    try_time = 0  # 设置登录尝试次数
    while True:
        try_time += 1
        if try_time == MAX_LOGIN_TRY_TIME:
            print("登录尝试次数过多，退尝试！")
            exit()
        print(f'{"-"*42}第[{try_time}]次登录尝试{"-"*42}')
        # 输入账号密码
        page.get_by_placeholder("你的手机号/邮箱").fill(USER_NAME)
        page.get_by_placeholder("密码").fill(USER_PASSWD)
        time.sleep(1)
        # 模拟鼠标点击登录按钮
        page.mouse.click(LOGIN_X, LOGIN_Y)
        time.sleep(3)
        print(page.title())
        # 截屏
        page.screenshot(full_page=True, path=os.path.join(PATH, 'B.png'))  # 截取整个页面，保存为B.png
        page.screenshot(path=os.path.join(PATH, 'head.png'), clip=HEAD_PIC)  # 截取上面的文字顺序，保存为head.png
        page.screenshot(path=os.path.join(PATH, 'content.png'), clip=CONTENT_PIC)  # 截取下面要点击的区域，保存为content.png
        # 识别
        ocr = OCR()
        pos, content = ocr()  # 返回要点击的位置与对应的内容（测试的时候看）
        print(pos, content)
        for p in pos:
            if p:
                # 模拟鼠标对应位置
                page.mouse.click(p[0]+CONTENT_X_START, p[1]+CONTENT_Y_START)
                time.sleep(3)
        page.mouse.click(CONFIRM_X, CONFIRM_Y)
        time.sleep(3)
        page.goto("https://space.bilibili.com")
        if '哔哩哔哩弹幕视频网' not in page.title():  # 判断是否成功登录
            print(page.title())
            break
        print('=' * 100)
    # 读取cookie相关信息
    required_cookie = ['SESSDATA', 'bili_jct', 'DedeUserID', 'DedeUserID__ckMd5', 'sid']
    cookie_lst = []
    for x in context.cookies():
        name = x.get('name', None)
        value = x.get('value', None)
        if name in required_cookie:
            cookie_lst.append(f'{name}={value}')
            required_cookie.remove(name)
    cookie_str = ';'.join(cookie_lst)
    print('Cookie:')
    print(cookie_str)
    # 将cookie放在文件cookie.txt中，默认路径和本文件路径相同
    with open(os.path.join(PATH, 'cookie.txt'), 'w') as f:
        f.write(cookie_str)
    os.popen(f'code {os.path.join(PATH, "cookie.txt")}')
    # 关闭爬虫浏览器
    context.close()
    browser.close()

with sync_playwright() as playwright:
    run(playwright)
    # 删除中间截图的图片
    os.remove(os.path.join(PATH, "B.png"))
    os.remove(os.path.join(PATH, "content.png"))
    os.remove(os.path.join(PATH, "head.png"))
