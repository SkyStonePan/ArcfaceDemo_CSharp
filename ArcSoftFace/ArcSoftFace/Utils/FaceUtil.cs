using System;
using ArcSoftFace.SDKModels;
using ArcSoftFace.SDKUtil;
using ArcSoftFace.Entity;
using System.Drawing;

namespace ArcSoftFace.Utils
{
    public class FaceUtil
    {
        /// <summary>
        /// 人脸检测(PS:检测RGB图像的人脸时，必须保证图像的宽度能被4整除，否则会失败)
        /// </summary>
        /// <param name="pEngine">引擎Handle</param>
        /// <param name="imageInfo">图像数据</param>
        /// <returns>人脸检测结果</returns>
        public static ASF_MultiFaceInfo DetectFace(IntPtr pEngine, ImageInfo imageInfo)
        {
            ASF_MultiFaceInfo multiFaceInfo = new ASF_MultiFaceInfo();
            IntPtr pMultiFaceInfo = MemoryUtil.Malloc(MemoryUtil.SizeOf<ASF_MultiFaceInfo>());
            int retCode = ASFFunctions.ASFDetectFaces(pEngine, imageInfo.width, imageInfo.height, imageInfo.format, imageInfo.imgData, pMultiFaceInfo);
            if (retCode != 0)
            {
                MemoryUtil.Free(pMultiFaceInfo);
                return multiFaceInfo;
            }
            multiFaceInfo = MemoryUtil.PtrToStructure<ASF_MultiFaceInfo>(pMultiFaceInfo);

            return multiFaceInfo;
        }

        /// <summary>
        /// 人脸检测
        /// </summary>
        /// <param name="pEngine">引擎Handle</param>
        /// <param name="image">图像</param>
        /// <returns>人脸检测结果</returns>
        private static object locks = new object();
        public static ASF_MultiFaceInfo DetectFace(IntPtr pEngine, Image image)
        {
            lock (locks)
            {
                ASF_MultiFaceInfo multiFaceInfo = new ASF_MultiFaceInfo();
                if (image != null)
                {
                    image = ImageUtil.ScaleImage(image, image.Width, image.Height);
                    ImageInfo imageInfo = ImageUtil.ReadBMP(image);
                    multiFaceInfo = DetectFace(pEngine, imageInfo);
                    MemoryUtil.Free(imageInfo.imgData);
                    return multiFaceInfo;

                }
                else
                {
                    return multiFaceInfo;
                }
            }
        }

        /// <summary>
        /// 提取人脸特征
        /// </summary>
        /// <param name="pEngine">引擎Handle</param>
        /// <param name="imageInfo">图像数据</param>
        /// <param name="multiFaceInfo">人脸检测结果</param>
        /// <returns>保存人脸特征结构体指针</returns>
        public static IntPtr ExtractFeature(IntPtr pEngine, ImageInfo imageInfo, ASF_MultiFaceInfo multiFaceInfo, out ASF_SingleFaceInfo singleFaceInfo)
        {
            singleFaceInfo = new ASF_SingleFaceInfo();
            singleFaceInfo.faceRect = MemoryUtil.PtrToStructure<MRECT>(multiFaceInfo.faceRects);
            singleFaceInfo.faceOrient = MemoryUtil.PtrToStructure<int>(multiFaceInfo.faceOrients);
            IntPtr pSingleFaceInfo = MemoryUtil.Malloc(MemoryUtil.SizeOf<ASF_SingleFaceInfo>());
            MemoryUtil.StructureToPtr(singleFaceInfo, pSingleFaceInfo);

            IntPtr pFaceFeature = MemoryUtil.Malloc(MemoryUtil.SizeOf<ASF_FaceFeature>());
            int retCode = ASFFunctions.ASFFaceFeatureExtract(pEngine, imageInfo.width, imageInfo.height, imageInfo.format, imageInfo.imgData, pSingleFaceInfo, pFaceFeature);
            Console.WriteLine("FR Extract Feature result:" + retCode);

            if (retCode != 0)
            {
                //释放指针
                MemoryUtil.Free(pSingleFaceInfo);
                MemoryUtil.Free(pFaceFeature);
                ASF_FaceFeature emptyFeature = new ASF_FaceFeature();
                IntPtr pEmptyFeature = MemoryUtil.Malloc(MemoryUtil.SizeOf<ASF_FaceFeature>());
                MemoryUtil.StructureToPtr(emptyFeature, pEmptyFeature);
                return pEmptyFeature;
            }

            //人脸特征feature过滤
            ASF_FaceFeature faceFeature = MemoryUtil.PtrToStructure<ASF_FaceFeature>(pFaceFeature);
            byte[] feature = new byte[faceFeature.featureSize];
            MemoryUtil.Copy(faceFeature.feature, feature, 0, faceFeature.featureSize);

            ASF_FaceFeature localFeature = new ASF_FaceFeature();
            localFeature.feature = MemoryUtil.Malloc(feature.Length);
            MemoryUtil.Copy(feature, 0, localFeature.feature, feature.Length);
            localFeature.featureSize = feature.Length;
            IntPtr pLocalFeature = MemoryUtil.Malloc(MemoryUtil.SizeOf<ASF_FaceFeature>());
            MemoryUtil.StructureToPtr(localFeature, pLocalFeature);

            //释放指针
            MemoryUtil.Free(pSingleFaceInfo);
            MemoryUtil.Free(pFaceFeature);

            return pLocalFeature;
        }

        /// <summary>
        /// 提取人脸特征
        /// </summary>
        /// <param name="pEngine">引擎Handle</param>
        /// <param name="image">图像</param>
        /// <returns>保存人脸特征结构体指针</returns>
        public static IntPtr ExtractFeature(IntPtr pEngine, Image image, out ASF_SingleFaceInfo singleFaceInfo)
        {
            image = ImageUtil.ScaleImage(image, image.Width, image.Height);
            ImageInfo imageInfo = ImageUtil.ReadBMP(image);
            ASF_MultiFaceInfo multiFaceInfo = DetectFace(pEngine, imageInfo);
            singleFaceInfo = new ASF_SingleFaceInfo();
            IntPtr pFaceModel = ExtractFeature(pEngine, imageInfo, multiFaceInfo, out singleFaceInfo);
            MemoryUtil.Free(imageInfo.imgData);
            return pFaceModel;
        }


        /// <summary>
        /// 提取单人脸特征
        /// </summary>
        /// <param name="pEngine">人脸识别引擎</param>
        /// <param name="image">图片</param>
        /// <param name="singleFaceInfo">单人脸信息</param>
        /// <returns>单人脸特征</returns>
        public static IntPtr ExtractFeature(IntPtr pEngine, Image image, ASF_SingleFaceInfo singleFaceInfo)
        {
            ImageInfo imageInfo = ImageUtil.ReadBMP(image);
            IntPtr pSingleFaceInfo = MemoryUtil.Malloc(MemoryUtil.SizeOf<ASF_SingleFaceInfo>());
            MemoryUtil.StructureToPtr(singleFaceInfo, pSingleFaceInfo);

            IntPtr pFaceFeature = MemoryUtil.Malloc(MemoryUtil.SizeOf<ASF_FaceFeature>());
            int retCode = -1;
            try
            {
                retCode = ASFFunctions.ASFFaceFeatureExtract(pEngine, imageInfo.width, imageInfo.height, imageInfo.format, imageInfo.imgData, pSingleFaceInfo, pFaceFeature);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.WriteLine("FR Extract Feature result:" + retCode);

            if (retCode != 0)
            {
                //释放指针
                MemoryUtil.Free(pSingleFaceInfo);
                MemoryUtil.Free(pFaceFeature);
                ASF_FaceFeature emptyFeature = new ASF_FaceFeature();
                IntPtr pEmptyFeature = MemoryUtil.Malloc(MemoryUtil.SizeOf<ASF_FaceFeature>());
                MemoryUtil.StructureToPtr(emptyFeature, pEmptyFeature);
                return pEmptyFeature;
            }

            //人脸特征feature过滤
            ASF_FaceFeature faceFeature = MemoryUtil.PtrToStructure<ASF_FaceFeature>(pFaceFeature);
            byte[] feature = new byte[faceFeature.featureSize];
            MemoryUtil.Copy(faceFeature.feature, feature, 0, faceFeature.featureSize);

            ASF_FaceFeature localFeature = new ASF_FaceFeature();
            localFeature.feature = MemoryUtil.Malloc(feature.Length);
            MemoryUtil.Copy(feature, 0, localFeature.feature, feature.Length);
            localFeature.featureSize = feature.Length;
            IntPtr pLocalFeature = MemoryUtil.Malloc(MemoryUtil.SizeOf<ASF_FaceFeature>());
            MemoryUtil.StructureToPtr(localFeature, pLocalFeature);

            //释放指针
            MemoryUtil.Free(pSingleFaceInfo);
            MemoryUtil.Free(pFaceFeature);
            MemoryUtil.Free(imageInfo.imgData);

            return pLocalFeature;
        }



        /// <summary>
        /// 年龄检测
        /// </summary>
        /// <param name="pEngine">引擎Handle</param>
        /// <param name="imageInfo">图像数据</param>
        /// <param name="multiFaceInfo">人脸检测结果</param>
        /// <returns>年龄检测结构体</returns>
        public static ASF_AgeInfo AgeEstimation(IntPtr pEngine, ImageInfo imageInfo, ASF_MultiFaceInfo multiFaceInfo, out int retCode)
        {
            retCode = -1;
            IntPtr pMultiFaceInfo = MemoryUtil.Malloc(MemoryUtil.SizeOf<ASF_MultiFaceInfo>());
            MemoryUtil.StructureToPtr(multiFaceInfo, pMultiFaceInfo);

            if (multiFaceInfo.faceNum == 0)
            {
                return new ASF_AgeInfo();
            }

            //人脸信息处理
            retCode = ASFFunctions.ASFProcess(pEngine, imageInfo.width, imageInfo.height, imageInfo.format, imageInfo.imgData, pMultiFaceInfo, FaceEngineMask.ASF_AGE);
            if (retCode == 0)
            {
                //获取年龄信息
                IntPtr pAgeInfo = MemoryUtil.Malloc(MemoryUtil.SizeOf<ASF_AgeInfo>());
                retCode = ASFFunctions.ASFGetAge(pEngine, pAgeInfo);
                Console.WriteLine("Get Age Result:" + retCode);
                ASF_AgeInfo ageInfo = MemoryUtil.PtrToStructure<ASF_AgeInfo>(pAgeInfo);

                //释放内存
                MemoryUtil.Free(pMultiFaceInfo);
                MemoryUtil.Free(pAgeInfo);
                return ageInfo;
            }
            else
            {
                return new ASF_AgeInfo();
            }
        }


        /// <summary>
        /// 单人脸年龄检测
        /// </summary>
        /// <param name="pEngine">人脸识别引擎</param>
        /// <param name="image">图片</param>
        /// <param name="singleFaceInfo">单人脸信息</param>
        /// <returns>年龄检测结果</returns>
        public static ASF_AgeInfo AgeEstimation(IntPtr pEngine, Image image, ASF_SingleFaceInfo singleFaceInfo)
        {
            ImageInfo imageInfo = ImageUtil.ReadBMP(image);
            ASF_MultiFaceInfo multiFaceInfo = new ASF_MultiFaceInfo();
            multiFaceInfo.faceRects = MemoryUtil.Malloc(MemoryUtil.SizeOf<MRECT>());
            MemoryUtil.StructureToPtr<MRECT>(singleFaceInfo.faceRect, multiFaceInfo.faceRects);
            multiFaceInfo.faceOrients = MemoryUtil.Malloc(MemoryUtil.SizeOf<int>());
            MemoryUtil.StructureToPtr<int>(singleFaceInfo.faceOrient, multiFaceInfo.faceOrients);
            multiFaceInfo.faceNum = 1;
            ASF_AgeInfo ageInfo = AgeEstimation(pEngine, imageInfo, multiFaceInfo);
            MemoryUtil.Free(imageInfo.imgData);
            return ageInfo;
        }

        /// <summary>
        /// 性别检测
        /// </summary>
        /// <param name="pEngine">引擎Handle</param>
        /// <param name="imageInfo">图像数据</param>
        /// <param name="multiFaceInfo">人脸检测结果</param>
        /// <returns>保存性别检测结果结构体</returns>
        public static ASF_GenderInfo GenderEstimation(IntPtr pEngine, ImageInfo imageInfo, ASF_MultiFaceInfo multiFaceInfo, out int retCode)
        {
            retCode = -1;
            IntPtr pMultiFaceInfo = MemoryUtil.Malloc(MemoryUtil.SizeOf<ASF_MultiFaceInfo>());
            MemoryUtil.StructureToPtr(multiFaceInfo, pMultiFaceInfo);

            if (multiFaceInfo.faceNum == 0)
            {
                return new ASF_GenderInfo();
            }

            //人脸信息处理
            retCode = ASFFunctions.ASFProcess(pEngine, imageInfo.width, imageInfo.height, imageInfo.format, imageInfo.imgData, pMultiFaceInfo, FaceEngineMask.ASF_GENDER);
            if (retCode == 0)
            {
                //获取性别信息
                IntPtr pGenderInfo = MemoryUtil.Malloc(MemoryUtil.SizeOf<ASF_GenderInfo>());
                retCode = ASFFunctions.ASFGetGender(pEngine, pGenderInfo);
                Console.WriteLine("Get Gender Result:" + retCode);
                ASF_GenderInfo genderInfo = MemoryUtil.PtrToStructure<ASF_GenderInfo>(pGenderInfo);

                //释放内存
                MemoryUtil.Free(pMultiFaceInfo);
                MemoryUtil.Free(pGenderInfo);

                return genderInfo;
            }
            else
            {
                return new ASF_GenderInfo();
            }
        }

        /// <summary>
        /// 人脸3D角度检测
        /// </summary>
        /// <param name="pEngine">引擎Handle</param>
        /// <param name="imageInfo">图像数据</param>
        /// <param name="multiFaceInfo">人脸检测结果</param>
        /// <returns>保存人脸3D角度检测结果结构体</returns>
        public static ASF_Face3DAngle Face3DAngleDetection(IntPtr pEngine, ImageInfo imageInfo, ASF_MultiFaceInfo multiFaceInfo,out int retCode)
        {
            IntPtr pMultiFaceInfo = MemoryUtil.Malloc(MemoryUtil.SizeOf<ASF_MultiFaceInfo>());
            MemoryUtil.StructureToPtr(multiFaceInfo, pMultiFaceInfo);

            if (multiFaceInfo.faceNum == 0)
            {
                retCode = -1;
                return new ASF_Face3DAngle();
            }

            //人脸信息处理
            retCode = ASFFunctions.ASFProcess(pEngine, imageInfo.width, imageInfo.height, imageInfo.format, imageInfo.imgData, pMultiFaceInfo, FaceEngineMask.ASF_FACE3DANGLE);
            if (retCode == 0)
            {
                //获取人脸3D角度
                IntPtr pFace3DAngleInfo = MemoryUtil.Malloc(MemoryUtil.SizeOf<ASF_Face3DAngle>());
                retCode = ASFFunctions.ASFGetFace3DAngle(pEngine, pFace3DAngleInfo);
                Console.WriteLine("Get Face3D Angle Result:" + retCode);
                ASF_Face3DAngle face3DAngle = MemoryUtil.PtrToStructure<ASF_Face3DAngle>(pFace3DAngleInfo);

                //释放内存
                MemoryUtil.Free(pMultiFaceInfo);
                MemoryUtil.Free(pFace3DAngleInfo);

                return face3DAngle;
            }else
            {
                return new ASF_Face3DAngle();
            }
        }


        /// <summary>
        /// 性别检测
        /// </summary>
        /// <param name="pEngine">引擎Handle</param>
        /// <param name="imageInfo">图像数据</param>
        /// <param name="multiFaceInfo">人脸检测结果</param>
        /// <returns>保存性别估计结果结构体</returns>
        public static ASF_GenderInfo GenderEstimation(IntPtr pEngine, ImageInfo imageInfo, ASF_MultiFaceInfo multiFaceInfo)
        {
            IntPtr pMultiFaceInfo = MemoryUtil.Malloc(MemoryUtil.SizeOf<ASF_MultiFaceInfo>());
            MemoryUtil.StructureToPtr(multiFaceInfo, pMultiFaceInfo);

            if (multiFaceInfo.faceNum == 0)
            {
                return new ASF_GenderInfo();
            }

            //人脸信息处理
            int retCode = ASFFunctions.ASFProcess(pEngine, imageInfo.width, imageInfo.height, imageInfo.format, imageInfo.imgData, pMultiFaceInfo, FaceEngineMask.ASF_GENDER);

            //获取性别信息
            IntPtr pGenderInfo = MemoryUtil.Malloc(MemoryUtil.SizeOf<ASF_GenderInfo>());
            retCode = ASFFunctions.ASFGetGender(pEngine, pGenderInfo);
            Console.WriteLine("Get Gender Result:" + retCode);
            ASF_GenderInfo genderInfo = MemoryUtil.PtrToStructure<ASF_GenderInfo>(pGenderInfo);

            //释放内存
            MemoryUtil.Free(pMultiFaceInfo);
            MemoryUtil.Free(pGenderInfo);

            return genderInfo;
        }



        /// <summary>
        /// 单人脸性别检测
        /// </summary>
        /// <param name="pEngine">引擎Handle</param>
        /// <param name="image">图片</param>
        /// <param name="singleFaceInfo">单人脸信息</param>
        /// <returns>性别估计结果</returns>
        public static ASF_GenderInfo GenderEstimation(IntPtr pEngine, Image image, ASF_SingleFaceInfo singleFaceInfo)
        {
            ImageInfo imageInfo = ImageUtil.ReadBMP(image);
            ASF_MultiFaceInfo multiFaceInfo = new ASF_MultiFaceInfo();
            multiFaceInfo.faceRects = MemoryUtil.Malloc(MemoryUtil.SizeOf<MRECT>());
            MemoryUtil.StructureToPtr<MRECT>(singleFaceInfo.faceRect, multiFaceInfo.faceRects);
            multiFaceInfo.faceOrients = MemoryUtil.Malloc(MemoryUtil.SizeOf<int>());
            MemoryUtil.StructureToPtr<int>(singleFaceInfo.faceOrient, multiFaceInfo.faceOrients);
            multiFaceInfo.faceNum = 1;
            ASF_GenderInfo genderInfo = GenderEstimation(pEngine, imageInfo, multiFaceInfo);
            MemoryUtil.Free(imageInfo.imgData);
            return genderInfo;
        }

        /// <summary>
        /// 年龄检测
        /// </summary>
        /// <param name="pEngine">引擎Handle</param>
        /// <param name="imageInfo">图像数据</param>
        /// <param name="multiFaceInfo">人脸检测结果</param>
        /// <returns>年龄检测结构体</returns>
        public static ASF_AgeInfo AgeEstimation(IntPtr pEngine, ImageInfo imageInfo, ASF_MultiFaceInfo multiFaceInfo)
        {
            IntPtr pMultiFaceInfo = MemoryUtil.Malloc(MemoryUtil.SizeOf<ASF_MultiFaceInfo>());
            MemoryUtil.StructureToPtr(multiFaceInfo, pMultiFaceInfo);

            if (multiFaceInfo.faceNum == 0)
            {
                return new ASF_AgeInfo();
            }

            //人脸信息处理
            int retCode = ASFFunctions.ASFProcess(pEngine, imageInfo.width, imageInfo.height, imageInfo.format, imageInfo.imgData, pMultiFaceInfo, FaceEngineMask.ASF_AGE);

            //获取年龄信息
            IntPtr pAgeInfo = MemoryUtil.Malloc(MemoryUtil.SizeOf<ASF_AgeInfo>());
            retCode = ASFFunctions.ASFGetAge(pEngine, pAgeInfo);
            Console.WriteLine("Get Age Result:" + retCode);
            ASF_AgeInfo ageInfo = MemoryUtil.PtrToStructure<ASF_AgeInfo>(pAgeInfo);

            //释放内存
            MemoryUtil.Free(pMultiFaceInfo);
            MemoryUtil.Free(pAgeInfo);

            return ageInfo;
        }

        /// <summary>
        /// 获取多个人脸检测结果中面积最大的人脸
        /// </summary>
        /// <param name="multiFaceInfo">人脸检测结果</param>
        /// <returns>面积最大的人脸信息</returns>
        public static ASF_SingleFaceInfo GetMaxFace(ASF_MultiFaceInfo multiFaceInfo)
        {
            ASF_SingleFaceInfo singleFaceInfo = new ASF_SingleFaceInfo();
            singleFaceInfo.faceRect = new MRECT();
            singleFaceInfo.faceOrient = 1;

            int maxArea = 0;
            int index = -1;
            for (int i = 0; i < multiFaceInfo.faceNum; i++)
            {
                MRECT rect = MemoryUtil.PtrToStructure<MRECT>(multiFaceInfo.faceRects + MemoryUtil.SizeOf<MRECT>() * i);
                int area = (rect.right - rect.left) * (rect.bottom - rect.top);
                if (maxArea <= area)
                {
                    maxArea = area;
                    index = i;
                }
            }
            if (index != -1)
            {
                singleFaceInfo.faceRect = MemoryUtil.PtrToStructure<MRECT>(multiFaceInfo.faceRects + MemoryUtil.SizeOf<MRECT>() * index);
                singleFaceInfo.faceOrient = MemoryUtil.PtrToStructure<int>(multiFaceInfo.faceOrients + MemoryUtil.SizeOf<int>() * index);
            }
            return singleFaceInfo;
        }
    }
}
