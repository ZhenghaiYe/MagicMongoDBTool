﻿using System;
using System.Collections.Generic;

namespace DarumaTool
{
    partial class IDL2PgmStruct
    {
        /// <summary>
        /// 重新整理Syntax
        /// </summary>
        /// <param name="SyntaxList"></param>
        private void ReSyntax(List<Syntax> SyntaxList)
        {
            //1.找到所有的IF，WHILE，CASE。然后，读到对应的结束的地方。
            byte currentNestLv;
            int branchNo = 1;
            for (int i = 0; i < SyntaxList.Count; i++)
            {
                switch (SyntaxList[i].SyntaxType)
                {
                    case "IF":
                        currentNestLv = SyntaxList[i].NestLv;
                        SyntaxSet IfSet = new SyntaxSet();
                        IfSet.SyntaxSetType = "IF";
                        IfSet.SyntaxList = new List<Syntax>();
                        Syntax newIf = GetResult(SyntaxList, i);
                        IfSet.SyntaxList.Add(newIf);
                        IfSet.SectionName = SyntaxList[i].SectionName;
                        IfSet.BranchNo = branchNo;
                        for (int ifcount = i; ifcount < SyntaxList.Count; ifcount++)
                        {
                            if ((SyntaxList[ifcount].NestLv == currentNestLv))
                            {
                                //寻找同层的直到 ELSE/END-IF
                                if (SyntaxList[ifcount].SyntaxType == "ELSE")
                                {
                                    Syntax newElse = GetResult(SyntaxList, ifcount);
                                    IfSet.SyntaxList.Add(newElse);
                                }
                                if (SyntaxList[ifcount].SyntaxType == "END-IF")
                                {
                                    break;
                                }
                            }
                            if ((SyntaxList[ifcount].NestLv == currentNestLv +1))
                            {
                                ///获得下层的ASSIGN备用
                                if (SyntaxList[ifcount].SyntaxType == "ASSIGN")
                                {
                                    if (String.IsNullOrEmpty(IfSet.SyntaxList[IfSet.SyntaxList.Count - 1].Result))
                                    {
                                        Syntax newsyntax = IfSet.SyntaxList[IfSet.SyntaxList.Count - 1];
                                        newsyntax.Result = SyntaxList[ifcount].ExtendInfo;
                                        IfSet.SyntaxList.RemoveAt(IfSet.SyntaxList.Count - 1);
                                        IfSet.SyntaxList.Add(newsyntax);
                                    }
                                }
                            }
                        }
                        Boolean OnlyIF = (IfSet.SyntaxList.Count == 1);
                        if (OnlyIF)
                        {
                            IfSet.SyntaxList.Add(new Syntax()
                            {
                                SyntaxType = "IF-NOT",
                                LineNo = IfSet.SyntaxList[0].LineNo,
                                NestLv = IfSet.SyntaxList[0].NestLv,
                                ExtendInfo = "上記以外",
                                Result = "次の処理を行う。"
                            });
                        }
                        else
                        {
                            Syntax elseSyntax = new Syntax();
                            elseSyntax = IfSet.SyntaxList[1];
                            elseSyntax.ExtendInfo = "上記以外";
                            IfSet.SyntaxList[1] = elseSyntax;
                        }
                        foreach (Section Section in SectionList)
                        {
                            if (Section.SectionName == IfSet.SectionName)
                            {
                                Section.SyntaxSetList.Add(IfSet);
                            }
                        }
                        break;
                    case "#IF":
                        currentNestLv = SyntaxList[i].NestLv;
                        SyntaxSet MacroIfSet = new SyntaxSet();
                        MacroIfSet.SyntaxSetType = "#IF";
                        MacroIfSet.SyntaxList = new List<Syntax>();
                        MacroIfSet.SyntaxList.Add(SyntaxList[i]);
                        MacroIfSet.SectionName = SyntaxList[i].SectionName;
                        MacroIfSet.BranchNo = branchNo;
                        //寻找同层的直到 END-IF
                        for (int ifcount = i; ifcount < SyntaxList.Count; ifcount++)
                        {
                            if ((SyntaxList[ifcount].NestLv == currentNestLv))
                            {
                                if (SyntaxList[ifcount].SyntaxType == "#ELSE")
                                {
                                    MacroIfSet.SyntaxList.Add(SyntaxList[ifcount]);
                                }
                                if (SyntaxList[ifcount].SyntaxType == "#END-IF")
                                {
                                    break;
                                }
                            }
                        }

                        foreach (Section Section in SectionList)
                        {
                            if (Section.SectionName == MacroIfSet.SectionName)
                            {
                                Section.SyntaxSetList.Add(MacroIfSet);
                            }
                        }
                        break;
                    case "WHILE":
                    case "REPEAT":
                    case "FOR":
                    case "LOOP":
                    case "GET":
                        currentNestLv = SyntaxList[i].NestLv;
                        SyntaxSet RepeatSet = new SyntaxSet();
                        RepeatSet.BranchNo = branchNo;
                        RepeatSet.SectionName = SyntaxList[i].SectionName;
                        RepeatSet.SyntaxSetType = SyntaxList[i].SyntaxType;
                        RepeatSet.SyntaxList = new List<Syntax>();
                        Syntax newWhile = GetResult(SyntaxList, i);
                        RepeatSet.SyntaxList.Add(newWhile);
                        //寻找同层的直到 END-WHILE
                        for (int whilecount = i; whilecount < SyntaxList.Count; whilecount++)
                        {
                            if ((SyntaxList[whilecount].NestLv == currentNestLv))
                            {
                                if (SyntaxList[whilecount].SyntaxType == ("END-" + SyntaxList[i].SyntaxType))
                                {
                                    break;
                                }
                            }
                        }
                        if (SyntaxList[i].SyntaxType != "GET")
                        {
                            RepeatSet.SyntaxList.Add(new Syntax()
                            {
                                SyntaxType = SyntaxList[i].SyntaxType + "-NOT",
                                LineNo = RepeatSet.SyntaxList[0].LineNo,
                                NestLv = RepeatSet.SyntaxList[0].NestLv,
                                ExtendInfo = "上記以外",
                                Result = "次の処理を行う。"
                            });
                        }
                        else
                        {
                            RepeatSet.SyntaxList.Add(new Syntax()
                            {
                                SyntaxType = "END-GET",
                                LineNo = RepeatSet.SyntaxList[0].LineNo,
                                NestLv = RepeatSet.SyntaxList[0].NestLv,
                                ExtendInfo = "上記以外",
                                Result = "次の処理を行う。"
                            });
                        }
                        foreach (Section Section in SectionList)
                        {
                            if (Section.SectionName == RepeatSet.SectionName)
                            {
                                Section.SyntaxSetList.Add(RepeatSet);
                            }
                        }
                        break;
                    case "CASE":
                        currentNestLv = SyntaxList[i].NestLv;
                        SyntaxSet CaseSet = new SyntaxSet();
                        CaseSet.BranchNo = branchNo;
                        CaseSet.SectionName = SyntaxList[i].SectionName;
                        CaseSet.SyntaxSetType = "CASE";
                        CaseSet.SyntaxList = new List<Syntax>();
                        CaseSet.SyntaxList.Add(SyntaxList[i]);
                        //寻找同层的直到 END-WHILE
                        Boolean HasCondtion = !string.IsNullOrEmpty(SyntaxList[i].ExtendInfo);
                        Boolean HasOther = false;
                        for (int casecount = i; casecount < SyntaxList.Count; casecount++)
                        {
                            if ((SyntaxList[casecount].NestLv == currentNestLv))
                            {
                                if (SyntaxList[casecount].SyntaxType == "WHEN")
                                {
                                    if (SyntaxList[casecount].ExtendInfo.Trim() == "OTHER")
                                    {
                                        HasOther = true;
                                        Syntax newWhen = GetResult(SyntaxList, casecount);
                                        CaseSet.SyntaxList.Add(newWhen);
                                    }
                                    else
                                    {
                                        if (!HasCondtion)
                                        {
                                            Syntax newWhen = GetResult(SyntaxList, casecount);
                                            newWhen.Cond = new clsCondition(newWhen.ExtendInfo);
                                            CaseSet.SyntaxList.Add(newWhen);
                                        }
                                        else
                                        {
                                            Syntax newWhen = GetResult(SyntaxList, casecount);
                                            CaseSet.SyntaxList.Add(newWhen);
                                        }
                                    }
                                }
                                if (SyntaxList[casecount].SyntaxType == "END-CASE")
                                {
                                    break;
                                }
                            }
                        }
                        if (!HasOther)
                        {
                            CaseSet.SyntaxList.Add(new Syntax()
                            {
                                SyntaxType = "WHEN",
                                LineNo = CaseSet.SyntaxList[0].LineNo,
                                NestLv = CaseSet.SyntaxList[0].NestLv,
                                ExtendInfo = "OTHER",
                                Result = "処理なし"
                            });
                        }
                        ///放入指定的Section中
                        foreach (Section Section in SectionList)
                        {
                            if (Section.SectionName == CaseSet.SectionName)
                            {
                                Section.SyntaxSetList.Add(CaseSet);
                            }
                        }
                        break;
                    case "CHECK":
                        currentNestLv = SyntaxList[i].NestLv;
                        SyntaxSet CheckSet = new SyntaxSet();
                        CheckSet.BranchNo = branchNo;
                        CheckSet.SectionName = SyntaxList[i].SectionName;
                        CheckSet.SyntaxSetType = "CHECK";
                        CheckSet.SyntaxList = new List<Syntax>();

                        String ExtendInfo = SyntaxList[i].ExtendInfo.Split(":".ToCharArray())[0];
                        String TorF = SyntaxList[i].ExtendInfo.Split(":".ToCharArray())[1];
                        Syntax Exist = SyntaxList[i];
                        Exist.ExtendInfo = ExtendInfo + "が　" + (TorF == "TRUE" ? "真" : "偽") + "　の場合";
                        CheckSet.SyntaxList.Add(Exist);
                        Syntax NOTExist = SyntaxList[i];
                        NOTExist = SyntaxList[i];
                        NOTExist.ExtendInfo = "上記以外";
                        NOTExist.Result = "処理なし";
                        CheckSet.SyntaxList.Add(NOTExist);
                        foreach (Section Section in SectionList)
                        {
                            if (Section.SectionName == CheckSet.SectionName)
                            {
                                Section.SyntaxSetList.Add(CheckSet);
                            }
                        }
                        break;
                    case "CALL":
                    case "MACRO":
                    case "ZGISTDOPN":
                    case "ZGISTDGET":
                    case "ZGISTDPUT":
                    case "ZGISTDCLS":
                    case "ZGIVSAOPN":
                    case "ZGIVSAGET":
                    case "ZGIVSAPUT":
                    case "ZGIVSACLS":
                        currentNestLv = SyntaxList[i].NestLv;
                        SyntaxSet CallSet = new SyntaxSet();
                        CallSet.SectionName = SyntaxList[i].SectionName;
                        CallSet.SyntaxSetType = SyntaxList[i].SyntaxType;
                        CallSet.SyntaxList = new List<Syntax>();
                        CallSet.SyntaxList.Add(SyntaxList[i]);
                        foreach (Section Section in SectionList)
                        {
                            if (Section.SectionName == CallSet.SectionName)
                            {
                                Section.SyntaxSetList.Add(CallSet);
                            }
                        }
                        branchNo--;
                        break;
                    default:
                        branchNo--;
                        break;
                }
                branchNo++;
            }
        }
        /// <summary>
        /// 生成期待结果
        /// </summary>
        /// <param name="SyntaxList"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private static Syntax GetResult(List<Syntax> SyntaxList, int i)
        {
            Syntax newSyntax = new Syntax();
            if (i != SyntaxList.Count - 1)
            {
                //如果下一个是CALL，阶层是IF的下一层，则Call/PERFROM作为IF的期待结果
                newSyntax = SyntaxList[i];
                for (int t = i; t < SyntaxList.Count - 1; t++)
                {
                    if (newSyntax.NestLv == SyntaxList[t + 1].NestLv - 1)
                    {
                        newSyntax = GetResultAfter(SyntaxList, t, newSyntax);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return newSyntax;
        }

        private static Syntax GetResultAfter(List<Syntax> SyntaxList, int t, Syntax newSyntax)
        {
            if (!String.IsNullOrEmpty(newSyntax.Result))
            {
                newSyntax.Result += "&";
            }
            switch (SyntaxList[t + 1].SyntaxType)
            {
                case "ABORT":
                    newSyntax.Result += "共通マクロ@ZGIAPABRTで、プログラムを中止させます";
                    break;
                case "CALL":
                    newSyntax.Result += "[%" + SyntaxList[t + 1].ExtendInfo.Trim() + "%]を呼出す";
                    break;
                case "MACRO":
                    newSyntax.Result += "[#" + SyntaxList[t + 1].ExtendInfo.Trim() + "#]を呼出す";
                    break;
                case "PERFORM":
                    newSyntax.Result += "[ " + SyntaxList[t + 1].ExtendInfo.Trim() + " ]へ遷移します";
                    break;
                case "ERROR":
                    newSyntax.Result += "[ " + SyntaxList[t + 1].ExtendInfo.Trim() + " ]へ遷移し、エラー処理を実行します";
                    break;
                case "IF":
                    //最后通过LineNo，替换出BranchNo
                    newSyntax.Result += "%" + SyntaxList[t + 1].LineNo + "% の分岐の判定の処理に移る";
                    break;
                case "CASE":
                    //最后通过LineNo，替换出BranchNo
                    newSyntax.Result += "%" + SyntaxList[t + 1].LineNo + "% の多分岐の判定の処理に移る";
                    break;
                case "FOR":
                case "LOOP":
                case "WHILE":
                case "REPEAT":
                    //最后通过LineNo，替换出BranchNo
                    newSyntax.Result += "%" + SyntaxList[t + 1].LineNo + "% のループの処理に移る";
                    break;
                default:
                    if (!String.IsNullOrEmpty(newSyntax.Result))
                    {
                        newSyntax.Result = newSyntax.Result.TrimEnd("&".ToCharArray());
                    }
                    break;
            }
            return newSyntax;
        }
        /// <summary>
        /// 重新整理SyntaxSet
        /// </summary>
        /// <param name="section"></param>
        private void ReSyntaxSet(ref Section section)
        {
            List<SyntaxSet> newSyntaxSet = new List<SyntaxSet>();
            if (section.SyntaxSetList.Count != 0)
            {
                for (int i = 0; i < section.SyntaxSetList.Count; i++)
                {
                    SyntaxSet CurrentSyntaxSet = section.SyntaxSetList[i];
                    switch (CurrentSyntaxSet.SyntaxSetType)
                    {
                        case "IF":
                        case "CASE":
                        case "LOOP":
                            //IF条件的简单判断
                            if (i != 0)
                            {
                                //如果IF具有上一个语句的话
                                SyntaxSet PreSyntax = section.SyntaxSetList[i - 1];
                                ExtendInfoByPreSyntax(ref CurrentSyntaxSet, ref PreSyntax);
                            }
                            break;
                        default:
                            break;
                    }
                    newSyntaxSet.Add(CurrentSyntaxSet);
                }
            }
            section.SyntaxSetList = newSyntaxSet;
        }

        private static void ExtendInfoByPreSyntax(ref SyntaxSet CurrentSyntaxSet, ref SyntaxSet PreSyntax)
        {
            if (CurrentSyntaxSet.SyntaxList[0].NestLv == PreSyntax.SyntaxList[0].NestLv)
            {
                //如果是同层次的CALL语句或者是FILE操作语句
                switch (PreSyntax.SyntaxSetType)
                {
                    case "CALL":
                        //CALL MODULE
                        CurrentSyntaxSet.ExtendInfo = "CALL:" + PreSyntax.SyntaxList[0].ExtendInfo;
                        break;
                    case "MACRO":
                        //CALL MODULE
                        CurrentSyntaxSet.ExtendInfo = "MACRO:" + PreSyntax.SyntaxList[0].ExtendInfo;
                        break;
                    case "ZGISTDOPN":
                    case "ZGISTDCLS":
                    case "ZGISTDPUT":
                    case "ZGISTDGET":
                        //FILE OPERATION
                        CurrentSyntaxSet.ExtendInfo = "FILE:" + PreSyntax.SyntaxSetType + ":" + PreSyntax.SyntaxList[0].ExtendInfo;
                        break;
                    case "ZGIVSAOPN":
                    case "ZGIVSACLS":
                    case "ZGIVSAPUT":
                    case "ZGIVSAGET":
                        //MASTER OPERATION
                        CurrentSyntaxSet.ExtendInfo = "MASTER:" + PreSyntax.SyntaxSetType + ":" + PreSyntax.SyntaxList[0].ExtendInfo;
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
