namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask.ThreeDaysSign;

/// <summary>
/// ThreeDaySignDto
/// </summary>
/// <param name="previous_vip_status"></param>
/// <param name="vip_status"></param>
/// <param name="day">周期内的第几天</param>
/// <param name="signed">今日是否已签到</param>
/// <param name="count">已累计签到天数</param>
/// <param name="has_coupon"></param>
/// <param name="countdown"></param>
/// <param name="score"></param>
/// <param name="vip_score"></param>
/// <param name="explain"></param>
/// <param name="exp_value"></param>
/// <param name="received_coupon"></param>
/// <param name="duration">累计签到周期</param>
public record ThreeDaySignDto(
    int previous_vip_status,
    int vip_status,
    int day,
    bool signed,
    int count,
    bool has_coupon,
    int countdown,
    int score,
    int vip_score,
    string explain,
    int exp_value,
    bool received_coupon,
    int duration
);
