using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos
{
    public class VideoDetail
    {
        public string Bvid { get; set; }

        public long Aid { get; set; }

        /// <summary>
        /// 稿件分P总数
        /// </summary>
        public int Videos { get; set; }

        /// <summary>
        /// 子分区名称
        /// </summary>
        public string Tname { get; set; }

        /// <summary>
        /// 是否转载
        /// <sample>1：原创</sample>
        /// <sample>2：转载</sample>
        /// </summary>
        public int Copyright { get; set; }

        /// <summary>
        /// 稿件封面图片url
        /// </summary>
        public string Pic { get; set; }

        /// <summary>
        /// 稿件标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 稿件发布时间(时间戳)
        /// </summary>
        public long Pubdate { get; set; }

        /// <summary>
        /// 用户提交稿件的时间(时间戳)
        /// </summary>
        public long Ctime { get; set; }

        /// <summary>
        /// 	视频简介
        /// </summary>
        public string Desc { get; set; }

        /// <summary>
        /// 视频状态
        /// </summary>
        public int State { get; set; }

        /// <summary>
        /// 稿件总时长（所有分P）(单位为秒)
        /// </summary>
        public long Duration { get; set; }
    }

    /*
     * 样例
     * {"code":0,"message":"0","ttl":1,"data":{"bvid":"BV1Bv411s7xN","aid":246364184,"videos":1,"tid":183,"tname":"影视剪辑","copyright":1,"pic":"http://i0.hdslb.com/bfs/archive/0355ba75255d4cfa67762ccd388fc6f90f010a3a.jpg","title":"美战？你说的是越战吧？","pubdate":1611656338,"ctime":1611656338,"desc":"小谢尔顿第一次邀请新朋友丹来家里吃饭。","state":0,"duration":164,"rights":{"bp":0,"elec":0,"download":1,"movie":0,"pay":0,"hd5":0,"no_reprint":1,"autoplay":1,"ugc_pay":0,"is_cooperation":0,"ugc_pay_preview":0,"no_background":0,"clean_mode":0,"is_stein_gate":0},"owner":{"mid":220893216,"name":"在7楼","face":"http://i0.hdslb.com/bfs/face/24cbaa418600c7872379d3f4cdb9e06cbb27c985.jpg"},"stat":{"aid":246364184,"view":52325,"danmaku":88,"reply":315,"favorite":200,"coin":1157,"share":412,"now_rank":0,"his_rank":0,"like":2251,"dislike":0,"evaluation":"","argue_msg":""},"dynamic":"","cid":287928175,"dimension":{"width":1280,"height":720,"rotate":0},"no_cache":false,"pages":[{"cid":287928175,"page":1,"from":"vupload","part":"welcome！你们一家为什么来德州呢？","duration":164,"vid":"","weblink":"","dimension":{"width":1280,"height":720,"rotate":0}}],"subtitle":{"allow_submit":false,"list":[]},"user_garb":{"url_image_ani_cut":""}}}
     */
}
