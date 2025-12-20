namespace RaidRecord.Core.ChatBot.Models;

public class ParaInfoBuilder
{
    private ParaInfo _paraInfo = new();

    /// <summary>
    /// 添加参数
    /// </summary>
    /// <param name="name">参数名</param>
    /// <param name="type">参数类型</param>
    /// <param name="desc">参数描述</param>
    public ParaInfoBuilder AddParam(string name, string type, string desc)
    {
        _paraInfo.Paras.Add(name);
        _paraInfo.Types[name] = type;
        _paraInfo.Descs[name] = desc;
        return this;
    }

    public ParaInfoBuilder SetOptional(string[] parameters)
    {
        foreach (string para in parameters)
        {
            _paraInfo.Optional.Add(para);
        }
        return this;
    }

    /// <summary>
    /// 构建参数信息, 返回构建好的实例后重置自身的数据
    /// </summary>
    public ParaInfo Build()
    {
        ParaInfo info = _paraInfo;
        _paraInfo = new ParaInfo();
        return info;
    }
}